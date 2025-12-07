using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class EmailVerification
{
    public int VerificationId { get; set; }
    
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(10)]
    public string OtpCode { get; set; } = string.Empty;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
}