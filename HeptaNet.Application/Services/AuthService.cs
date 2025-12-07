using HeptaNet.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HeptaNet.Application.Services.Otp;
using HeptaNet.Domain.Base;

namespace HeptaNet.Application.Services;

public interface IAuthService
{
    Task<(bool Success, string? Token, User? User, string? ErrorMessage)>
        LoginAsync(string email, string password);
    string GenerateJwtToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    Task<(bool Success, string? Message)> ForgotPasswordAsync(string email);
    Task<(bool Success, string? Message)> ResetPasswordAsync(string email, string otpCode, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IOtpService _otpService;
    private readonly IAuthorizedEmailRepository _authorizedEmailRepository;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IOtpService otpService,
        IAuthorizedEmailRepository authorizedEmailRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _otpService = otpService;
        _authorizedEmailRepository = authorizedEmailRepository;
    }

    public async Task<(bool Success, string? Token, User? User, string? ErrorMessage)>
        LoginAsync(string email, string password)
    {
        try
        {
            // 1. Trouver l'utilisateur par email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, null, null, "Aucun utilisateur créé avec cet email");
            }

            // 2. Vérifier si l'utilisateur est blacklisté
            if (user.IsBlacklisted)
            {
                return (false, null, null, "Votre compte a été suspendu. Contactez le support.");
            }

            // 3. Vérifier le mot de passe
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                password,
                false,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return (false, null, null, "Compte verrouillé temporairement. Veuillez réessayer plus tard.");
                }
                return (false, null, null, "Mot de passe incorrect");
            }

            // 4. Mettre à jour la dernière connexion
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

              // 5. Verifier si email existe dans AuthorizedEmails et mettre a jour IsImported to true
            try
            {
                var authorizedEmail = await _authorizedEmailRepository.GetByEmailAsync(email);
                if (authorizedEmail != null && !authorizedEmail.IsImported)
                {
                    await _authorizedEmailRepository.UpdateImportStatusAsync(email, true);
                    _logger.LogInformation("Email {Email} marqué comme importé dans AuthorizedEmails", email);
                }
            }
            catch (Exception ex)
            {
            // Log probleme aveec l'email dans AuthorizedEmails
                _logger.LogWarning(ex, "Impossible de mettre à jour le statut IsImported pour {Email}", email);
            }


            // 6. Générer le token JWT
            var token = GenerateJwtToken(user);

            return (true, token, user, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion de {Email}", email);
            return (false, null, null, "Une erreur interne s'est produite");
        }
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim("IsBlacklisted", user.IsBlacklisted.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Ajouter le rôle depuis Identity si disponible
        var userRoles = _userManager.GetRolesAsync(user).Result;
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey!);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Message)> ForgotPasswordAsync(string email)
    {
        try
        {
            // Vérifier si l'utilisateur existe
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Pour des raisons de sécurité, on ne révèle pas si l'email existe ou non
                return (true, "Si cet email existe, un code de réinitialisation a été envoyé.");
            }

            // Vérifier si l'utilisateur est blacklisté
            if (user.IsBlacklisted)
            {
                return (false, "Ce compte a été suspendu. Contactez le support.");
            }

            // Envoyer le code OTP avec le purpose "password-reset"
            var result = await _otpService.SendAsync(email, "password-reset");
            
            if (!result.Success)
            {
                if (result.IsCooldown)
                {
                    return (false, $"Veuillez patienter {result.CooldownSeconds} secondes avant de réessayer.");
                }
                return (false, result.Message ?? "Erreur lors de l'envoi du code.");
            }

            return (true, "Un code de réinitialisation a été envoyé à votre adresse email.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la demande de réinitialisation de mot de passe pour {Email}", email);
            return (false, "Une erreur interne s'est produite. Veuillez réessayer plus tard.");
        }
    }

    public async Task<(bool Success, string? Message)> ResetPasswordAsync(string email, string otpCode, string newPassword)
    {
        try
        {
            // Vérifier si l'utilisateur existe
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, "Aucun utilisateur créé avec cet email");
            }

            // Vérifier si l'utilisateur est blacklisté
            if (user.IsBlacklisted)
            {
                return (false, "Ce compte a été suspendu. Contactez le support.");
            }

            // Vérifier le code OTP
            var verifyResult = await _otpService.VerifyAsync(email, "password-reset", otpCode);
            if (!verifyResult.Valid)
            {
                if (verifyResult.Expired)
                {
                    return (false, "Le code OTP a expiré. Veuillez demander un nouveau code.");
                }
                return (false, verifyResult.Message ?? "Code OTP incorrect. Veuillez réessayer.");
            }

            // Réinitialiser le mot de passe
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                // Extraire les messages d'erreur spécifiques
                var errorMessages = result.Errors.Select(e => 
                {
                    if (e.Code == "PasswordTooShort")
                        return "Le mot de passe doit contenir au moins 6 caractères";
                    if (e.Code == "PasswordRequiresNonAlphanumeric")
                        return "Le mot de passe doit contenir au moins un caractère spécial";
                    if (e.Code == "PasswordRequiresDigit")
                        return "Le mot de passe doit contenir au moins un chiffre";
                    if (e.Code == "PasswordRequiresUpper")
                        return "Le mot de passe doit contenir au moins une majuscule";
                    if (e.Code == "PasswordRequiresLower")
                        return "Le mot de passe doit contenir au moins une minuscule";
                    return e.Description;
                });
                return (false, $"Erreur lors de la réinitialisation : {string.Join(", ", errorMessages)}");
            }

            return (true, "Votre mot de passe a été réinitialisé avec succès.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation de mot de passe pour {Email}", email);
            return (false, "Une erreur interne s'est produite. Veuillez réessayer plus tard.");
        }
    }
}