using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public enum ConversationType
{
    Private = 0,
    Group = 1
}

public class Conversation
{
    public int ConversationId { get; set; }
    
    public ConversationType Type { get; set; }
    
    // For private chats: both users; For groups: null (use GroupId)
    public int? User1Id { get; set; }
    public int? User2Id { get; set; }
    
    // For group conversations
    public int? GroupId { get; set; }
    
    // Denormalized fields for performance (updated on each new message)
    public int? LastMessageId { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int? LastMessageSenderId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    public virtual User? User1 { get; set; }
    public virtual User? User2 { get; set; }
    public virtual Group? Group { get; set; }
    public virtual Message? LastMessage { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
}


