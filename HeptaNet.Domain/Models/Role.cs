using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class Role
{
    public int RoleId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Navigation property
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}