using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models;
namespace HeptaNet.Domain.Dto;
public class SendOtpDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
    public string Email { get; set; } = string.Empty;
}

public class VerifyOtpDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le code OTP est requis")]
    [StringLength(10, ErrorMessage = "Le code OTP ne peut pas dépasser 10 caractères")]
    public string OtpCode { get; set; } = string.Empty;
}

public class EmailVerificationResponseDto
{
    public int VerificationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
