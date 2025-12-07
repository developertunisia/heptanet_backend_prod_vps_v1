
using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public enum BlacklistActionType
{
    Blacklisted = 0,
    Unblacklisted = 1
}

public class BlacklistLog
{
    public int LogId { get; set; }
    
    public int UserId { get; set; }
    
    public int ActionBy { get; set; }
    
    public BlacklistActionType ActionType { get; set; }
    
    public DateTime DateAction { get; set; } = DateTime.UtcNow;
    
    [StringLength(500)]
    public string? Reason { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual User ActionByUser { get; set; } = null!;
}