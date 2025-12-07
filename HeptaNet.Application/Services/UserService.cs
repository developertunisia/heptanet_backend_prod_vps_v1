using HeptaNet.Domain.Models;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Base;
using Microsoft.AspNetCore.Identity;
using HeptaNet.Domain.Constants;
using System;

namespace HeptaNet.Application.Services;

// Interface du service utilisateur
public interface IUserService
{
    Task<UserResponseDto?> GetUserByIdAsync(int id);
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync(bool? excludeBlacklisted = null, string? roleName = null);
    Task<IEnumerable<UserResponseDto>> GetActiveUsersAsync();
    Task<UserResponseDto> RegisterUserAsync(RegisterUserDto registerUserDto);
    Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<bool> DeleteUserAsync(int id);
    Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string searchTerm);
    Task<bool> BlacklistUserAsync(BlacklistUserDto blacklistUserDto);
    Task<bool> UnblacklistUserAsync(int userId);
}

// Implémentation du service utilisateur
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, UserManager<User> userManager, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user != null ? await MapToResponseDto(user) : null;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync(bool? excludeBlacklisted = null, string? roleName = null)
    {
        IEnumerable<User> users;
        
        // Appliquer le filtre blacklist si spécifié
        if (excludeBlacklisted == true)
        {
            users = await _userRepository.GetActiveUsersAsync();
        }
        else
        {
            users = await _userRepository.GetAllAsync();
        }
        
        var responseDtos = new List<UserResponseDto>();
        
        foreach (var user in users)
        {
            var userDto = await MapToResponseDto(user);
            
            // Appliquer le filtre par rôle si spécifié
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                if (userDto.Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
                {
                    responseDtos.Add(userDto);
                }
            }
            else
            {
                responseDtos.Add(userDto);
            }
        }
        
        return responseDtos;
    }

    public async Task<IEnumerable<UserResponseDto>> GetActiveUsersAsync()
    {
        var users = await _userRepository.GetActiveUsersAsync();
        var responseDtos = new List<UserResponseDto>();
        
        foreach (var user in users)
        {
            responseDtos.Add(await MapToResponseDto(user));
        }
        
        return responseDtos;
    }

    public async Task<UserResponseDto> RegisterUserAsync(RegisterUserDto registerUserDto)
    {
        // Vérifier si l'email existe déjà
        var existingUser = await _userManager.FindByEmailAsync(registerUserDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Un compte existe déjà avec cet email. Veuillez vous connecter.");
        }

        var user = new User
        {
            FirstName = registerUserDto.FirstName,
            LastName = registerUserDto.LastName,
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email, // Identity requires UserName
            WhatsAppNumber = registerUserDto.WhatsAppNumber,
            // ❌ NE PLUS UTILISER RoleId
            // RoleId = registerUserDto.RoleId,
            CreatedAt = DateTime.UtcNow,
            IsBlacklisted = false
        };

        // ✅ Créer l'utilisateur avec UserManager
        var result = await _userManager.CreateAsync(user, registerUserDto.Password);
        
        if (!result.Succeeded)
        {
            // Extraire les messages d'erreur spécifiques
            var errorMessages = result.Errors.Select(e => 
            {
                if (e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail")
                    return "Un compte existe déjà avec cet email";
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
                if (e.Code == "InvalidEmail")
                    return "Adresse email incorrecte";
                return e.Description;
            });
            throw new InvalidOperationException($"Erreur lors de la création du compte : {string.Join(", ", errorMessages)}");
        }

        // ✅ Assigner le rôle via Identity
        var roleName = string.IsNullOrEmpty(registerUserDto.RoleName) 
            ? AppRoles.Utilisateur  // Par défaut
            : registerUserDto.RoleName;
        
        // Vérifier que le rôle existe et est valide
        if (!AppRoles.AllRoles.Contains(roleName))
        {
            roleName = AppRoles.Utilisateur; // Fallback
        }
        
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        
        if (!roleResult.Succeeded)
        {
            // Si l'ajout du rôle échoue, supprimer l'utilisateur créé
            await _userManager.DeleteAsync(user);
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Erreur lors de l'assignation du rôle : {errors}");
        }

        // Récupérer l'utilisateur créé
        var createdUser = await _userManager.FindByEmailAsync(registerUserDto.Email);
        if (createdUser == null)
        {
            throw new InvalidOperationException("Erreur : l'utilisateur n'a pas pu être récupéré après création.");
        }
        return await MapToResponseDto(createdUser);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return null;

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.WhatsAppNumber = updateUserDto.WhatsAppNumber;

        var updatedUser = await _userRepository.UpdateAsync(user);
        if (updatedUser == null)
        {
            throw new InvalidOperationException("Erreur : l'utilisateur n'a pas pu être mis à jour.");
        }
        return await MapToResponseDto(updatedUser);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        return await _userRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string searchTerm)
    {
        var users = await _userRepository.SearchUsersAsync(searchTerm);
        var responseDtos = new List<UserResponseDto>();
        
        foreach (var user in users)
        {
            responseDtos.Add(await MapToResponseDto(user));
        }
        
        return responseDtos;
    }

    public async Task<bool> BlacklistUserAsync(BlacklistUserDto blacklistUserDto)
    {
        var user = await _userRepository.GetByIdAsync(blacklistUserDto.UserId);
        if (user == null)
            return false;

        user.IsBlacklisted = true;
        var updatedUser = await _userRepository.UpdateAsync(user);
        if (updatedUser == null)
        {
            return false;
        }
        
        // Log the blacklist action
        var blacklistLog = new BlacklistLog
        {
            UserId = blacklistUserDto.UserId,
            ActionBy = 1, // TODO: Get from current user context
            ActionType = BlacklistActionType.Blacklisted,
            Reason = blacklistUserDto.Reason,
            DateAction = DateTime.UtcNow
        };

        // TODO: Add blacklist log to repository
        return true;
    }

    public async Task<bool> UnblacklistUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        user.IsBlacklisted = false;
        var updatedUser = await _userRepository.UpdateAsync(user);
        if (updatedUser == null)
        {
            return false;
        }
        
        // Log the unblacklist action
        var blacklistLog = new BlacklistLog
        {
            UserId = userId,
            ActionBy = 1, // TODO: Get from current user context
            ActionType = BlacklistActionType.Unblacklisted,
            DateAction = DateTime.UtcNow
        };

        // TODO: Add blacklist log to repository
        return true;
    }

    private async Task<UserResponseDto> MapToResponseDto(User user)
    {
        // ✅ Récupérer les rôles depuis Identity
        var roles = await _userManager.GetRolesAsync(user);
        
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            WhatsAppNumber = user.WhatsAppNumber,
            Roles = roles.ToList(),
            IsBlacklisted = user.IsBlacklisted,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }
}
