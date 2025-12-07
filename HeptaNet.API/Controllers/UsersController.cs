using Microsoft.AspNetCore.Mvc;
using HeptaNet.Domain.Dto;
using HeptaNet.Application.Services;
using Microsoft.AspNetCore.Authorization;
using HeptaNet.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using HeptaNet.Domain.Models;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService, 
        UserManager<User> userManager,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les utilisateurs avec filtres optionnels (blacklist et rôle)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers(
        [FromQuery] bool? excludeBlacklisted = null,
        [FromQuery] string? roleName = null)
    {
        try
        {
            var users = await _userService.GetAllUsersAsync(excludeBlacklisted, roleName);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs");
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Récupère tous les utilisateurs actifs
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetActiveUsers()
    {
        try
        {
            var users = await _userService.GetActiveUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des utilisateurs actifs");
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Récupère un utilisateur par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound($"Utilisateur avec l'ID {id} non trouvé");
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Recherche des utilisateurs par terme de recherche
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> SearchUsers([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Le terme de recherche ne peut pas être vide");
            }

            var users = await _userService.SearchUsersAsync(searchTerm);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la recherche d'utilisateurs avec le terme: {SearchTerm}", searchTerm);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Crée un nouvel utilisateur
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> RegisterUser([FromBody] RegisterUserDto registerUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { success = false, message = "Champs erronés. Veuillez vérifier vos informations.", errors });
            }

            var user = await _userService.RegisterUserAsync(registerUserDto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement de l'utilisateur");
            return StatusCode(500, new { success = false, message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Met à jour un utilisateur existant
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { message = "Champs erronés. Veuillez vérifier vos informations.", errors });
            }

            var user = await _userService.UpdateUserAsync(id, updateUserDto);
            if (user == null)
            {
                return NotFound($"Utilisateur avec l'ID {id} non trouvé");
            }

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour de l'utilisateur {UserId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Supprime un utilisateur
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound($"Utilisateur avec l'ID {id} non trouvé");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur {UserId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Blackliste un utilisateur
    /// </summary>
    [HttpPost("blacklist")]
    public async Task<ActionResult> BlacklistUser([FromBody] BlacklistUserDto blacklistUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { message = "Champs erronés. Veuillez vérifier vos informations.", errors });
            }

            var result = await _userService.BlacklistUserAsync(blacklistUserDto);
            if (!result)
            {
                return NotFound($"Utilisateur avec l'ID {blacklistUserDto.UserId} non trouvé");
            }

            return Ok(new { message = "Utilisateur blacklisté avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la blacklist de l'utilisateur {UserId}", blacklistUserDto.UserId);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Retire un utilisateur de la blacklist
    /// </summary>
    [HttpPost("{userId}/unblacklist")]
    public async Task<ActionResult> UnblacklistUser(int userId)
    {
        try
        {
            var result = await _userService.UnblacklistUserAsync(userId);
            if (!result)
            {
                return NotFound($"Utilisateur avec l'ID {userId} non trouvé");
            }

            return Ok(new { message = "Utilisateur retiré de la blacklist avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du retrait de la blacklist de l'utilisateur {UserId}", userId);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Assigne un rôle à un utilisateur (SuperAdmin uniquement)
    /// </summary>
    [HttpPost("{userId}/assign-role")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult> AssignRole(int userId, [FromBody] AssignRoleDto assignRoleDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound($"Utilisateur avec l'ID {userId} non trouvé");
            }

            // Vérifier que le rôle est valide
            if (!AppRoles.AllRoles.Contains(assignRoleDto.RoleName))
            {
                return BadRequest($"Rôle '{assignRoleDto.RoleName}' invalide");
            }

            // Retirer les anciens rôles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Ajouter le nouveau rôle
            var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = $"Rôle '{assignRoleDto.RoleName}' assigné avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'assignation du rôle à l'utilisateur {UserId}", userId);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Récupère les rôles d'un utilisateur
    /// </summary>
    [HttpGet("{userId}/roles")]
    public async Task<ActionResult<List<string>>> GetUserRoles(int userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound($"Utilisateur avec l'ID {userId} non trouvé");
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des rôles de l'utilisateur {UserId}", userId);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }
}
