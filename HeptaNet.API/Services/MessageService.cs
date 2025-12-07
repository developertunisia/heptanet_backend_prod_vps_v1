using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using HeptaNet.API.Hubs;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Models;
using HeptaNet.Infrastructure.Database;
using HeptaNet.Application.Services.FileStorage;
using HeptaNet.Application.Services.Audio;
using Microsoft.AspNetCore.Hosting;

namespace HeptaNet.API.Services;

public interface IMessageService
{
    Task<Message> SendMessageAsync(SendMessageDto dto, int senderId, IFormFile? audioFile = null);
    Task<Message?> GetMessageByIdAsync(int messageId);
    Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 50);
    Task<IEnumerable<MessageWithAttachmentsDto>> GetConversationMessagesWithAttachmentsAsync(int conversationId, int userId, string baseUrl, int page = 1, int pageSize = 50);
    Task MarkAsReadAsync(int messageId, int userId);
    Task MarkConversationAsReadAsync(int conversationId, int userId);
    Task<Message> EditMessageAsync(int messageId, string newContent, int userId);
    Task DeleteMessageAsync(int messageId, int userId);
}

public class MessageService : IMessageService
{
    private readonly MyAppContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessageService> _logger;
    private readonly IFileStorageService _fileStorageService;
    private readonly IAudioMetadataService _audioMetadataService;
    private readonly IWebHostEnvironment _environment;
    
    public MessageService(
        MyAppContext context, 
        IHubContext<ChatHub> hubContext,
        ILogger<MessageService> logger,
        IFileStorageService fileStorageService,
        IAudioMetadataService audioMetadataService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _audioMetadataService = audioMetadataService;
        _environment = environment;
    }
    
    public async Task<Message> SendMessageAsync(SendMessageDto dto, int senderId, IFormFile? audioFile = null)
    {
        // Create the message
        var message = new Message
        {
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            ReceiverId = dto.ReceiverId,
            GroupId = dto.GroupId,
            Type = dto.Type,
            Content = dto.Content,
            ReplyToMessageId = dto.ReplyToMessageId,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        
        // Si c'est un message vocal, uploader le fichier
        if (audioFile != null && dto.Type == MessageType.Audio)
        {
            // Uploader le fichier
            var fileUrl = await _fileStorageService.SaveFileAsync(
                audioFile, 
                "voice-messages", 
                senderId);
            
            // Extraire la durée si possible
            var duration = await _audioMetadataService.GetAudioDurationAsync(audioFile);
            
            // Créer l'attachment
            var attachment = new MessageAttachment
            {
                MessageId = message.MessageId,
                FileName = audioFile.FileName,
                ContentType = audioFile.ContentType,
                FileUrl = fileUrl,
                FileSize = audioFile.Length,
                DurationSeconds = duration
            };
            
            _context.MessageAttachments.Add(attachment);
            
            // Mettre à jour le Content du message avec la durée
            if (duration.HasValue)
            {
                message.Content = $"🎤 Message vocal ({duration}s)";
            }
            else
            {
                message.Content = "🎤 Message vocal";
            }
        }
        
        // Update conversation last message
        var conversation = await _context.Conversations.FindAsync(dto.ConversationId);
        if (conversation != null)
        {
            conversation.LastMessageId = message.MessageId;
            conversation.LastMessageContent = message.Content.Length > 100 
                ? message.Content.Substring(0, 100) + "..." 
                : message.Content;
            conversation.LastMessageAt = message.CreatedAt;
            conversation.LastMessageSenderId = senderId;
            conversation.UpdatedAt = DateTime.UtcNow;
        }
        
        // Update unread counts for participants (except sender)
        var participants = await _context.ConversationParticipants
            .Where(cp => cp.ConversationId == dto.ConversationId && cp.UserId != senderId)
            .ToListAsync();
            
        foreach (var participant in participants)
        {
            participant.UnreadCount++;
            participant.LastActivityAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        
        // Load sender information and attachments for the notification
        var sender = await _context.Users.FindAsync(senderId);
        
        // Load attachments
        var attachments = await _context.MessageAttachments
            .Where(a => a.MessageId == message.MessageId)
            .ToListAsync();
        
        var messageDto = new MessageReceivedDto
        {
            MessageId = message.MessageId,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown",
            SenderAvatar = sender?.AvatarUrl,
            Type = message.Type,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            ReplyToMessageId = message.ReplyToMessageId,
            Status = message.Status,
            Attachments = attachments.Select(a => new MessageAttachmentDto
            {
                AttachmentId = a.AttachmentId,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileUrl = a.FileUrl,
                FullFileUrl = a.FileUrl, // Sera mis à jour dans le contrôleur avec l'URL complète
                FileSize = a.FileSize,
                DurationSeconds = a.DurationSeconds
            }).ToList()
        };
        
        // Send via SignalR
        if (dto.GroupId.HasValue)
        {
            // Send to all group members
            await _hubContext.Clients.Group($"group_{dto.GroupId}")
                .SendAsync("ReceiveGroupMessage", signalRDto);
        }
        else if (dto.ReceiverId.HasValue)
        {
            // Send to specific receiver
            await _hubContext.Clients.Group($"user_{dto.ReceiverId}")
                .SendAsync("ReceivePrivateMessage", signalRDto);
        }
        
        // Also broadcast to conversation group
        await _hubContext.Clients.Group($"conversation_{dto.ConversationId}")
            .SendAsync("NewMessage", signalRDto);
        
        _logger.LogInformation($"Message {message.MessageId} sent by user {senderId} in conversation {dto.ConversationId}");
        
        // Return response DTO (no circular references)
        return new MessageResponseDto
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown",
            ReceiverId = message.ReceiverId,
            ReceiverName = receiver != null ? $"{receiver.FirstName} {receiver.LastName}" : null,
            GroupId = message.GroupId,
            GroupName = group?.Name,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            Status = message.Status
        };
    }
    
    public async Task<MessageResponseDto?> GetMessageByIdAsync(int messageId)
    {
        return await _context.Messages
            .Where(m => m.MessageId == messageId)
            .Select(m => new MessageResponseDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver != null ? $"{m.Receiver.FirstName} {m.Receiver.LastName}" : null,
                GroupId = m.GroupId,
                GroupName = m.Group != null ? m.Group.Name : null,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Status = m.Status
            })
            .FirstOrDefaultAsync();
    }
    
    public async Task<IEnumerable<MessageResponseDto>> GetConversationMessagesAsync(
        int conversationId, 
        int userId, 
        int page = 1, 
        int pageSize = 50)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageResponseDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver != null ? $"{m.Receiver.FirstName} {m.Receiver.LastName}" : null,
                GroupId = m.GroupId,
                GroupName = m.Group != null ? m.Group.Name : null,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Status = m.Status
            })
            .ToListAsync();
    }
    
    public async Task<IEnumerable<MessageWithAttachmentsDto>> GetConversationMessagesWithAttachmentsAsync(
        int conversationId, 
        int userId, 
        string baseUrl,
        int page = 1, 
        int pageSize = 50)
    {
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.ReplyToMessage)
            .ToListAsync();
        
        return messages.Select(m => new MessageWithAttachmentsDto
        {
            MessageId = m.MessageId,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
            SenderAvatar = m.Sender?.AvatarUrl,
            Type = m.Type,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            EditedAt = m.EditedAt,
            ReplyToMessageId = m.ReplyToMessageId,
            Status = m.Status,
            Attachments = m.Attachments.Select(a => new MessageAttachmentDto
            {
                AttachmentId = a.AttachmentId,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileUrl = a.FileUrl,
                FullFileUrl = $"{baseUrl.TrimEnd('/')}{a.FileUrl}", // URL complète pour accès direct
                FileSize = a.FileSize,
                DurationSeconds = a.DurationSeconds
            }).ToList()
        });
    }
    
    public async Task MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
            
        if (message == null || message.SenderId == userId)
            return;
        
        // Update or create message receipt
        var receipt = await _context.MessageReceipts
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
            
        if (receipt == null)
        {
            receipt = new MessageReceipt
            {
                MessageId = messageId,
                UserId = userId,
                Status = ReceiptStatus.Read,
                ReadAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.MessageReceipts.Add(receipt);
        }
        else if (receipt.Status != ReceiptStatus.Read)
        {
            receipt.Status = ReceiptStatus.Read;
            receipt.ReadAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        
        // Notify sender via SignalR
        await _hubContext.Clients.Group($"user_{message.SenderId}")
            .SendAsync("MessageRead", new MessageReadReceiptDto
            {
                MessageId = messageId,
                UserId = userId,
                UserName = await GetUserNameAsync(userId),
                ReadAt = receipt.ReadAt ?? DateTime.UtcNow
            });
    }
    
    public async Task MarkConversationAsReadAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
            
        if (participant != null)
        {
            participant.UnreadCount = 0;
            participant.LastActivityAt = DateTime.UtcNow;
            
            // Get the last message in the conversation
            var lastMessage = await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
                
            if (lastMessage != null)
            {
                participant.LastReadMessageId = lastMessage.MessageId;
            }
            
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<Message> EditMessageAsync(int messageId, string newContent, int userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        
        if (message == null)
            throw new InvalidOperationException("Message not found");
            
        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("You can only edit your own messages");
        
        message.Content = newContent;
        message.EditedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Notify via SignalR
        await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
            .SendAsync("MessageEdited", new
            {
                MessageId = messageId,
                ConversationId = message.ConversationId,
                NewContent = newContent,
                EditedAt = message.EditedAt
            });
        
        return message;
    }
    
    public async Task DeleteMessageAsync(int messageId, int userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        
        if (message == null)
            throw new InvalidOperationException("Message not found");
            
        if (message.SenderId != userId)
            throw new UnauthorizedAccessException("You can only delete your own messages");
        
        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        // Notify via SignalR
        await _hubContext.Clients.Group($"conversation_{message.ConversationId}")
            .SendAsync("MessageDeleted", new
            {
                MessageId = messageId,
                ConversationId = message.ConversationId,
                DeletedAt = message.DeletedAt
            });
    }
    
    private async Task<string> GetUserNameAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }
}


