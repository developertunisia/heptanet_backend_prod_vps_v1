using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models;
namespace HeptaNet.Domain.Dto;

// ==================== BROADCAST MESSAGE DTOs ====================
public class CreateBroadcastDto
{
    [Required(ErrorMessage = "Le titre est requis")]
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le contenu est requis")]
    public string Content { get; set; } = string.Empty;
}

public class BroadcastResponseDto
{
    public int BroadcastId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ==================== MESSAGE DTOs (Enhanced for SignalR) ====================
public class SendMessageDto
{
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Le contenu du message est requis")]
    public string Content { get; set; } = string.Empty;
    
    public int? ReceiverId { get; set; }
    
    public int? GroupId { get; set; }
    
    public MessageType Type { get; set; } = MessageType.Text;
    
    public int? ReplyToMessageId { get; set; }
}

public class UpdateMessageStatusDto
{
    [Required(ErrorMessage = "L'ID du message est requis")]
    public int MessageId { get; set; }
    
    [Required(ErrorMessage = "Le statut est requis")]
    public MessageStatus Status { get; set; }
}

public class MessageResponseDto
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int? ReceiverId { get; set; }
    public string? ReceiverName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public MessageStatus Status { get; set; }
}

public class MessageReceivedDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? ReplyToMessageId { get; set; }
    public MessageStatus Status { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
}

public class MessageAttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FullFileUrl { get; set; } = string.Empty; // URL complète pour accès direct
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}

public class MessageWithAttachmentsDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public int? ReplyToMessageId { get; set; }
    public MessageStatus Status { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
    public bool HasAudio => Attachments.Any(a => a.ContentType.StartsWith("audio/"));
    public MessageAttachmentDto? AudioAttachment => Attachments.FirstOrDefault(a => a.ContentType.StartsWith("audio/"));
}

// ==================== CONVERSATION DTOs (Enhanced for SignalR) ====================
public class ConversationDto
{
    public int ConversationId { get; set; }
    public ConversationType Type { get; set; }
    public string? ConversationName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPinned { get; set; }
    public int? OtherUserId { get; set; } // For private chats
    public int? GroupId { get; set; } // For group chats
}

public class CreateConversationDto
{
    public ConversationType Type { get; set; }
    public int? OtherUserId { get; set; } // For private conversations
    public int? GroupId { get; set; } // For group conversations
}

// ==================== TYPING & STATUS DTOs ====================
public class TypingIndicatorDto
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsTyping { get; set; }
}

public class MessageStatusUpdateDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public MessageStatus Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class MessageReadReceiptDto
{
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ReadAt { get; set; }
}