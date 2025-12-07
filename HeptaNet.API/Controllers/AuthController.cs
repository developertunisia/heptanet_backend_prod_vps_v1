using HeptaNet.Domain.Dto;
using HeptaNet.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto loginRequest)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Champs erronés. Veuillez vérifier vos informations.",
                    Errors = errors
                });
            }

            var (success, token, user, errorMessage) = await _authService.LoginAsync(
                loginRequest.Email,
                loginRequest.Password);

            if (!success || user == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = errorMessage ?? "Erreur de connexion"
                });
            }

            // Récupérer les informations complètes de l'utilisateur avec les rôles
            var userDto = await _userService.GetUserByIdAsync(user.Id);
            
            if (userDto == null)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Erreur lors de la récupération des informations utilisateur"
                });
            }

            // ✅ Retourner la réponse structurée avec les rôles
            var response = new LoginResponseDto
            {
                AccessToken = token!,
                RefreshToken = null,
                User = new LoginUserInfoDto
                {
                    Id = userDto.Id,
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    IsBlacklisted = userDto.IsBlacklisted,
                    Roles = userDto.Roles,
                    RoleName = userDto.RoleName
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la connexion de l'utilisateur {Email}", loginRequest.Email);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Une erreur interne s'est produite"
            });
        }
    }

    [Authorize]
    [HttpGet("verify")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyToken()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Utilisateur non authentifié"
            });
        }

        var userDto = await _userService.GetUserByIdAsync(userId);
        if (userDto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Utilisateur introuvable"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Statut utilisateur récupéré",
            Data = new
            {
                isBlacklisted = userDto.IsBlacklisted,
                user = userDto
            }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public ActionResult<ApiResponse<object>> Logout()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Déconnexion effectuée"
        });
    }

    /// <summary>
    /// Demande de réinitialisation de mot de passe - Envoie un code OTP par email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Champs erronés. Veuillez vérifier vos informations.",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                        .ToList()
                });
            }

            var (success, message) = await _authService.ForgotPasswordAsync(dto.Email);

            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = message ?? "Erreur lors de l'envoi du code"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = message ?? "Code envoyé avec succès"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la demande de réinitialisation pour {Email}", dto.Email);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Une erreur interne s'est produite"
            });
        }
    }

    /// <summary>
    /// Réinitialisation de mot de passe avec code OTP
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Champs erronés. Veuillez vérifier vos informations.",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                        .ToList()
                });
            }

            var (success, message) = await _authService.ResetPasswordAsync(
                dto.Email,
                dto.OtpCode,
                dto.NewPassword);

            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = message ?? "Erreur lors de la réinitialisation"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = message ?? "Mot de passe réinitialisé avec succès"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation pour {Email}", dto.Email);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Une erreur interne s'est produite"
            });
        }
    }
}