using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class User : IdentityUser<int>
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? WhatsAppNumber { get; set; }
    
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    
    public bool IsBlacklisted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    
    // Privacy settings
    public bool ShowOnlineStatus { get; set; } = true;
    public bool ShowReadReceipts { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public virtual ICollection<BroadcastMessage> BroadcastMessages { get; set; } = new List<BroadcastMessage>();
    public virtual ICollection<BlacklistLog> BlacklistLogs { get; set; } = new List<BlacklistLog>();
    public virtual ICollection<Group> CreatedGroups { get; set; } = new List<Group>();
    public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
    public virtual ICollection<MessageReceipt> MessageReceipts { get; set; } = new List<MessageReceipt>();
}