using HeptaNet.Domain.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HeptaNet.Application.Services;

// Interface du service rôle
public interface IRoleService
{
    Task<RoleResponseDto?> GetRoleByIdAsync(int id);
    Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync();
    Task<RoleResponseDto> CreateRoleAsync(CreateRoleDto createRoleDto);
    Task<RoleResponseDto?> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto);
    Task<bool> DeleteRoleAsync(int id);
    Task<IEnumerable<RoleResponseDto>> SearchRolesAsync(string searchTerm);
    Task<bool> RoleExistsAsync(int id);
    Task<bool> RoleNameExistsAsync(string name, int? excludeId = null);
    Task<int> GetUserCountByRoleAsync(string roleName);
}

// Implémentation du service rôle
public class RoleService : IRoleService
{
    private readonly RoleManager<IdentityRole<int>> _roleManager;
    private readonly UserManager<HeptaNet.Domain.Models.User> _userManager;

    public RoleService(
        RoleManager<IdentityRole<int>> roleManager,
        UserManager<HeptaNet.Domain.Models.User> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<RoleResponseDto?> GetRoleByIdAsync(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        return role != null ? await MapToResponseDto(role) : null;
    }

    public async Task<IEnumerable<RoleResponseDto>> GetAllRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        var responseDtos = new List<RoleResponseDto>();
        
        foreach (var role in roles)
        {
            responseDtos.Add(await MapToResponseDto(role));
        }
        
        return responseDtos;
    }

    public async Task<RoleResponseDto> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        // Vérifier si le nom du rôle existe déjà
        var existingRole = await _roleManager.FindByNameAsync(createRoleDto.Name);
        if (existingRole != null)
        {
            throw new InvalidOperationException($"Un rôle avec le nom '{createRoleDto.Name}' existe déjà.");
        }

        var role = new IdentityRole<int>
        {
            Name = createRoleDto.Name
            // Note : IdentityRole ne supporte pas Description par défaut
            // Pour ajouter Description, vous devrez créer une classe personnalisée qui hérite de IdentityRole<int>
        };

        var result = await _roleManager.CreateAsync(role);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Erreur lors de la création du rôle : {errors}");
        }

        return await MapToResponseDto(role);
    }

    public async Task<RoleResponseDto?> UpdateRoleAsync(int id, UpdateRoleDto updateRoleDto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return null;

        // Vérifier si le nom du rôle existe déjà pour un autre rôle
        var existingRole = await _roleManager.FindByNameAsync(updateRoleDto.Name);
        if (existingRole != null && existingRole.Id != id)
        {
            throw new InvalidOperationException($"Un rôle avec le nom '{updateRoleDto.Name}' existe déjà.");
        }

        role.Name = updateRoleDto.Name;

        var result = await _roleManager.UpdateAsync(role);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Erreur lors de la mise à jour du rôle : {errors}");
        }

        return await MapToResponseDto(role);
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
            return false;

        // Vérifier s'il y a des utilisateurs avec ce rôle
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
        {
            throw new InvalidOperationException($"Impossible de supprimer le rôle car {usersInRole.Count} utilisateur(s) l'utilisent encore.");
        }

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded;
    }

    public async Task<IEnumerable<RoleResponseDto>> SearchRolesAsync(string searchTerm)
    {
        var roles = await _roleManager.Roles
            .Where(r => r.Name != null && r.Name.Contains(searchTerm))
            .ToListAsync();
            
        var responseDtos = new List<RoleResponseDto>();
        
        foreach (var role in roles)
        {
            responseDtos.Add(await MapToResponseDto(role));
        }
        
        return responseDtos;
    }

    public async Task<bool> RoleExistsAsync(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        return role != null;
    }

    public async Task<bool> RoleNameExistsAsync(string name, int? excludeId = null)
    {
        var existingRole = await _roleManager.FindByNameAsync(name);
        if (existingRole == null)
            return false;

        // Si on exclut un ID spécifique (pour les mises à jour)
        if (excludeId.HasValue && existingRole.Id == excludeId.Value)
            return false;

        return true;
    }

    public async Task<int> GetUserCountByRoleAsync(string roleName)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        return usersInRole.Count;
    }

    private async Task<RoleResponseDto> MapToResponseDto(IdentityRole<int> role)
    {
        var userCount = 0;
        if (role.Name != null)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
            userCount = usersInRole.Count;
        }
        
        return new RoleResponseDto
        {
            RoleId = role.Id,
            Name = role.Name ?? string.Empty,
            Description = null, // Identity roles n'ont pas de description par défaut
            UserCount = userCount
        };
    }
}