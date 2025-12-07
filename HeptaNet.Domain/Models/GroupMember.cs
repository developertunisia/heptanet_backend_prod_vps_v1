using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public enum GroupRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}

public class GroupMember
{
    public int Id { get; set; }
    
    public int GroupId { get; set; }
    public int UserId { get; set; }
    
    public GroupRole Role { get; set; } = GroupRole.Member;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Who added this member
    public int? AddedBy { get; set; }
    
    // Navigation properties
    public virtual Group Group { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? AddedByUser { get; set; }
}