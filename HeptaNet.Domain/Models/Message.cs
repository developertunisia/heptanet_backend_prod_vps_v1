
using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public enum MessageType
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
    Document = 4,
    Location = 5,
    Contact = 6,
    Sticker = 7,
    System = 8  // "User joined", "User left", etc.
}

public enum MessageStatus
{
    Sending = 0,
    Sent = 1,
    Delivered = 2,
    Read = 3,
    Failed = 4
}

public class Message
{
    public int MessageId { get; set; }
    
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    
    // For backward compatibility with direct messages
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    
    public MessageType Type { get; set; } = MessageType.Text;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    
    // Reply/Thread support
    public int? ReplyToMessageId { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    public virtual User? Receiver { get; set; }
    public virtual Group? Group { get; set; }
    public virtual Message? ReplyToMessage { get; set; }
    
    public virtual ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
    public virtual ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
}
public class BroadcastMessage
{
    public int BroadcastId { get; set; }
    
    public int SenderId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual User Sender { get; set; } = null!;
}
