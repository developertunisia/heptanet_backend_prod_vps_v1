using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class ConversationParticipant
{
    public int Id { get; set; }
    
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    
    // Last message the user has read
    public int? LastReadMessageId { get; set; }
    
    // Unread count (denormalized for performance)
    public int UnreadCount { get; set; } = 0;
    
    // User-specific settings
    public bool IsMuted { get; set; } = false;
    public bool IsPinned { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    
    // When user deleted/left the conversation
    public DateTime? DeletedAt { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Message? LastReadMessage { get; set; }
}


