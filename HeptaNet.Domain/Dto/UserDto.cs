using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models;
namespace HeptaNet.Domain.Dto;

public class RegisterUserDto
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir entre 8 et 100 caractères")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [StringLength(20, ErrorMessage = "Le numéro WhatsApp ne peut pas dépasser 20 caractères")]
    public string? WhatsAppNumber { get; set; }
    
    // ✅ Rendre optionnel - si non fourni, "Utilisateur" par défaut
    public string? RoleName { get; set; }
}

public class LoginUserDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
}

public class UpdateUserDto
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nom est requis")]
    [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = string.Empty;
    
    [StringLength(20, ErrorMessage = "Le numéro WhatsApp ne peut pas dépasser 20 caractères")]
    public string? WhatsAppNumber { get; set; }
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Le mot de passe actuel est requis")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir entre 8 et 100 caractères")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    
    // ✅ Supporter les deux systèmes pendant la transition
    [Obsolete("Use Roles instead")]
    public int? RoleId { get; set; }
    
    // ✅ Liste des rôles de l'utilisateur
    public List<string> Roles { get; set; } = new List<string>();
    
    // ✅ Rôle principal (le premier de la liste)
    public string RoleName => Roles.FirstOrDefault() ?? "Utilisateur";
    
    public bool IsBlacklisted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class BlacklistUserDto
{
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    public int UserId { get; set; }
    
    [StringLength(500, ErrorMessage = "La raison ne peut pas dépasser 500 caractères")]
    public string? Reason { get; set; }
}

public class AssignRoleDto
{
    [Required(ErrorMessage = "Le nom du rôle est requis")]
    public string RoleName { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la requête de login
/// </summary>
public class LoginRequestDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le mot de passe est requis")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// DTO pour la réponse de login
/// </summary>
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public LoginUserInfoDto User { get; set; } = null!;
}

/// <summary>
/// DTO pour les informations utilisateur dans la réponse de login
/// </summary>
public class LoginUserInfoDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
    public string RoleName { get; set; } = "Utilisateur";
}

/// <summary>
/// DTO pour la demande de réinitialisation de mot de passe
/// </summary>
public class ForgotPasswordDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour la réinitialisation de mot de passe avec OTP
/// </summary>
public class ResetPasswordDto
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le code OTP est requis")]
    public string OtpCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir entre 8 et 100 caractères")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}