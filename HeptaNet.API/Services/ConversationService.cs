using Microsoft.EntityFrameworkCore;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Models;
using HeptaNet.Infrastructure.Database;

namespace HeptaNet.API.Services;

public interface IConversationService
{
    Task<Conversation> CreateOrGetPrivateConversationAsync(int user1Id, int user2Id);
    Task<Conversation> CreateGroupConversationAsync(int groupId);
    Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId, bool includeArchived = false);
    Task<Conversation?> GetConversationByIdAsync(int conversationId, int userId);
    Task<ConversationDto?> GetConversationDtoByIdAsync(int conversationId, int userId);
    Task ArchiveConversationAsync(int conversationId, int userId);
    Task UnarchiveConversationAsync(int conversationId, int userId);
    Task PinConversationAsync(int conversationId, int userId);
    Task UnpinConversationAsync(int conversationId, int userId);
    Task MuteConversationAsync(int conversationId, int userId);
    Task UnmuteConversationAsync(int conversationId, int userId);
    Task DeleteConversationAsync(int conversationId, int userId);
}

public class ConversationService : IConversationService
{
    private readonly MyAppContext _context;
    private readonly ILogger<ConversationService> _logger;
    
    public ConversationService(MyAppContext context, ILogger<ConversationService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<Conversation> CreateOrGetPrivateConversationAsync(int user1Id, int user2Id)
    {
        // Check if conversation already exists (in either direction)
        var existingConversation = await _context.Conversations
            .FirstOrDefaultAsync(c => 
                c.Type == ConversationType.Private &&
                ((c.User1Id == user1Id && c.User2Id == user2Id) ||
                 (c.User1Id == user2Id && c.User2Id == user1Id)) &&
                !c.IsDeleted);
        
        if (existingConversation != null)
            return existingConversation;
        
        // Create new conversation
        var conversation = new Conversation
        {
            Type = ConversationType.Private,
            User1Id = user1Id,
            User2Id = user2Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        
        // Add participants
        var participants = new[]
        {
            new ConversationParticipant
            {
                ConversationId = conversation.ConversationId,
                UserId = user1Id,
                JoinedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            },
            new ConversationParticipant
            {
                ConversationId = conversation.ConversationId,
                UserId = user2Id,
                JoinedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            }
        };
        
        _context.ConversationParticipants.AddRange(participants);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Created private conversation {conversation.ConversationId} between users {user1Id} and {user2Id}");
        
        return conversation;
    }
    
    public async Task<Conversation> CreateGroupConversationAsync(int groupId)
    {
        // Check if conversation already exists for this group
        var existingConversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.GroupId == groupId && !c.IsDeleted);
        
        if (existingConversation != null)
            return existingConversation;
        
        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
        
        // Add all group members as participants
        var groupMembers = await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId && gm.IsActive)
            .ToListAsync();
        
        var participants = groupMembers.Select(gm => new ConversationParticipant
        {
            ConversationId = conversation.ConversationId,
            UserId = gm.UserId,
            JoinedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        });
        
        _context.ConversationParticipants.AddRange(participants);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Created group conversation {conversation.ConversationId} for group {groupId}");
        
        return conversation;
    }
    
    public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(int userId, bool includeArchived = false)
    {
        var query = _context.ConversationParticipants
            .Where(cp => cp.UserId == userId && cp.DeletedAt == null);
        
        if (!includeArchived)
        {
            query = query.Where(cp => !cp.IsArchived);
        }
        
        var participants = await query
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.User1)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.User2)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Group)
            .OrderByDescending(cp => cp.IsPinned)
            .ThenByDescending(cp => cp.Conversation.LastMessageAt)
            .ToListAsync();
        
        return participants.Select(cp => new ConversationDto
        {
            ConversationId = cp.ConversationId,
            Type = cp.Conversation.Type,
            ConversationName = GetConversationName(cp.Conversation, userId),
            AvatarUrl = GetConversationAvatar(cp.Conversation, userId),
            LastMessageContent = cp.Conversation.LastMessageContent,
            LastMessageAt = cp.Conversation.LastMessageAt,
            UnreadCount = cp.UnreadCount,
            IsMuted = cp.IsMuted,
            IsPinned = cp.IsPinned,
            OtherUserId = cp.Conversation.Type == ConversationType.Private 
                ? (cp.Conversation.User1Id == userId ? cp.Conversation.User2Id : cp.Conversation.User1Id)
                : null,
            GroupId = cp.Conversation.GroupId
        });
    }
    
    public async Task<Conversation?> GetConversationByIdAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (!participant)
            return null;
        
        return await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Group)
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
    }
    
    public async Task<ConversationDto?> GetConversationDtoByIdAsync(int conversationId, int userId)
    {
        var participantData = await _context.ConversationParticipants
            .Where(cp => cp.ConversationId == conversationId && cp.UserId == userId)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.User1)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.User2)
            .Include(cp => cp.Conversation)
                .ThenInclude(c => c.Group)
            .FirstOrDefaultAsync();
        
        if (participantData == null)
            return null;
        
        var conversation = participantData.Conversation;
        
        return new ConversationDto
        {
            ConversationId = conversation.ConversationId,
            Type = conversation.Type,
            ConversationName = GetConversationName(conversation, userId),
            AvatarUrl = GetConversationAvatar(conversation, userId),
            LastMessageContent = conversation.LastMessageContent,
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = participantData.UnreadCount,
            IsMuted = participantData.IsMuted,
            IsPinned = participantData.IsPinned,
            OtherUserId = conversation.Type == ConversationType.Private 
                ? (conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id)
                : null,
            GroupId = conversation.GroupId
        };
    }
    
    public async Task ArchiveConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsArchived = true;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UnarchiveConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsArchived = false;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task PinConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsPinned = true;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UnpinConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsPinned = false;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task MuteConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsMuted = true;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UnmuteConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant != null)
        {
            participant.IsMuted = false;
            participant.LastActivityAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteConversationAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
        
        if (participant == null)
        {
            throw new InvalidOperationException("Conversation not found or access denied");
        }
        
        // Soft delete - set DeletedAt timestamp
        participant.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"User {userId} deleted conversation {conversationId}");
    }
    
    private string GetConversationName(Conversation conversation, int currentUserId)
    {
        if (conversation.Type == ConversationType.Group)
        {
            return conversation.Group?.Name ?? "Group Chat";
        }
        
        // Private conversation
        var otherUser = conversation.User1Id == currentUserId ? conversation.User2 : conversation.User1;
        return otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "Private Chat";
    }
    
    private string? GetConversationAvatar(Conversation conversation, int currentUserId)
    {
        if (conversation.Type == ConversationType.Group)
        {
            return conversation.Group?.AvatarUrl;
        }
        
        // Private conversation
        var otherUser = conversation.User1Id == currentUserId ? conversation.User2 : conversation.User1;
        return otherUser?.AvatarUrl;
    }
}


