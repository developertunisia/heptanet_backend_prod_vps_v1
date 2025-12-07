using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HeptaNet.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    
    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }
    
    // Connection management
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Notify others user is online
            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserOnline", userId);
            
            _logger.LogInformation($"User {userId} connected with connection {Context.ConnectionId}");
        }
        
        await base.OnConnectedAsync();
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Notify others user is offline
            await Clients.AllExcept(Context.ConnectionId).SendAsync("UserOffline", userId);
            
            _logger.LogInformation($"User {userId} disconnected");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
    
    // Send private message
    public async Task SendPrivateMessage(int receiverId, string message, int conversationId)
    {
        var senderId = GetCurrentUserId();
        
        // Send to receiver's group
        await Clients.Group($"user_{receiverId}").SendAsync("ReceivePrivateMessage", new
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
    
    // Send group message
    public async Task SendGroupMessage(int groupId, string message, int conversationId)
    {
        var senderId = GetCurrentUserId();
        
        // Broadcast to all group members
        await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", new
        {
            ConversationId = conversationId,
            GroupId = groupId,
            SenderId = senderId,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
    
    // Join a group conversation
    public async Task JoinGroup(int groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} joined group {groupId}");
    }
    
    // Leave a group conversation
    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} left group {groupId}");
    }
    
    // Join a conversation
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} joined conversation {conversationId}");
    }
    
    // Leave a conversation
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        
        var userId = GetCurrentUserId();
        _logger.LogInformation($"User {userId} left conversation {conversationId}");
    }
    
    // Typing indicator
    public async Task UserTyping(int conversationId, bool isGroup = false, int? groupId = null)
    {
        var userId = GetCurrentUserId();
        
        if (isGroup && groupId.HasValue)
        {
            await Clients.GroupExcept($"group_{groupId}", Context.ConnectionId)
                .SendAsync("UserTyping", conversationId, userId);
        }
        else
        {
            // For private chat, send to conversation participants
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserTyping", conversationId, userId);
        }
    }
    
    // Stop typing indicator
    public async Task UserStoppedTyping(int conversationId, bool isGroup = false, int? groupId = null)
    {
        var userId = GetCurrentUserId();
        
        if (isGroup && groupId.HasValue)
        {
            await Clients.GroupExcept($"group_{groupId}", Context.ConnectionId)
                .SendAsync("UserStoppedTyping", conversationId, userId);
        }
        else
        {
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", conversationId, userId);
        }
    }
    
    // Mark message as read
    public async Task MarkAsRead(int messageId, int conversationId, int senderId)
    {
        var userId = GetCurrentUserId();
        
        // Notify sender that their message was read
        await Clients.Group($"user_{senderId}").SendAsync("MessageRead", new
        {
            MessageId = messageId,
            ConversationId = conversationId,
            ReadBy = userId,
            ReadAt = DateTime.UtcNow
        });
    }
    
    // Message delivered notification
    public async Task MessageDelivered(int messageId, int senderId)
    {
        var userId = GetCurrentUserId();
        
        await Clients.Group($"user_{senderId}").SendAsync("MessageDelivered", new
        {
            MessageId = messageId,
            DeliveredTo = userId,
            DeliveredAt = DateTime.UtcNow
        });
    }
    
    // Notify message edited
    public async Task NotifyMessageEdited(int messageId, int conversationId, string newContent)
    {
        await Clients.Group($"conversation_{conversationId}").SendAsync("MessageEdited", new
        {
            MessageId = messageId,
            ConversationId = conversationId,
            NewContent = newContent,
            EditedAt = DateTime.UtcNow
        });
    }
    
    // Notify message deleted
    public async Task NotifyMessageDeleted(int messageId, int conversationId)
    {
        await Clients.Group($"conversation_{conversationId}").SendAsync("MessageDeleted", new
        {
            MessageId = messageId,
            ConversationId = conversationId,
            DeletedAt = DateTime.UtcNow
        });
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}

