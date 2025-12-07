using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class Group
{
    public int GroupId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Group settings
    public int MaxMembers { get; set; } = 256;
    
    // Who can send messages
    public bool OnlyAdminsCanPost { get; set; } = false;
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    
    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    public virtual Conversation? Conversation { get; set; }
}
