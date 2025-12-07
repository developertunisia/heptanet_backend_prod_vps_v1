using Microsoft.AspNetCore.Mvc;
using HeptaNet.Domain.Dto;
using HeptaNet.Application.Services;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les rôles
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetAllRoles()
    {
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des rôles");
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Récupère un rôle par son ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RoleResponseDto>> GetRole(int id)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
            {
                return NotFound($"Rôle avec l'ID {id} non trouvé");
            }
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération du rôle {RoleId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Recherche des rôles par terme de recherche
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<RoleResponseDto>>> SearchRoles([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Le terme de recherche ne peut pas être vide");
            }

            var roles = await _roleService.SearchRolesAsync(searchTerm);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la recherche de rôles avec le terme: {SearchTerm}", searchTerm);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Crée un nouveau rôle
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RoleResponseDto>> CreateRole([FromBody] CreateRoleDto createRoleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _roleService.CreateRoleAsync(createRoleDto);
            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du rôle");
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Met à jour un rôle existant
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RoleResponseDto>> UpdateRole(int id, [FromBody] UpdateRoleDto updateRoleDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _roleService.UpdateRoleAsync(id, updateRoleDto);
            if (role == null)
            {
                return NotFound($"Rôle avec l'ID {id} non trouvé");
            }

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la mise à jour du rôle {RoleId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Supprime un rôle
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        try
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (!result)
            {
                return NotFound($"Rôle avec l'ID {id} non trouvé");
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression du rôle {RoleId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Vérifie si un rôle existe
    /// </summary>
    [HttpGet("{id}/exists")]
    public async Task<ActionResult<bool>> RoleExists(int id)
    {
        try
        {
            var exists = await _roleService.RoleExistsAsync(id);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'existence du rôle {RoleId}", id);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }

    /// <summary>
    /// Vérifie si un nom de rôle existe
    /// </summary>
    [HttpGet("name-exists")]
    public async Task<ActionResult<bool>> RoleNameExists([FromQuery] string name, [FromQuery] int? excludeId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Le nom du rôle ne peut pas être vide");
            }

            var exists = await _roleService.RoleNameExistsAsync(name, excludeId);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'existence du nom de rôle {RoleName}", name);
            return StatusCode(500, "Une erreur interne s'est produite");
        }
    }
}
