using HeptaNet.Domain.Base;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Models;
using HeptaNet.Domain.Query;
using HeptaNet.Domain.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HeptaNet.Application.Services;

public interface IAuthorizedEmailService
{
    Task<AuthorizedEmailResponseDto> CreateAuthorizedEmailAsync(CreateAuthorizedEmailRequest request);
    Task<AuthorizedEmailCheckResponseDto> CheckEmailExistsAsync(CheckEmailExistsRequest request);
    Task<AuthorizedEmailStatusUpdateResponseDto> ActivateEmailAsync(UpdateEmailStatusRequest request);
    Task<AuthorizedEmailStatusUpdateResponseDto> DeactivateEmailAsync(UpdateEmailStatusRequest request);
    Task<bool> DeleteEmailAsync(DeleteEmailRequest request);
    Task<IEnumerable<AuthorizedEmailResponseDto>> GetAllAsync();
    Task<IEnumerable<AuthorizedEmailQueryResult>> SearchAsync(GetAuthorizedEmailQuery query);
}

public class AuthorizedEmailService : IAuthorizedEmailService
{
    private readonly IAuthorizedEmailRepository _repository;
    private readonly ILogger<AuthorizedEmailService> _logger;

    public AuthorizedEmailService(
        IAuthorizedEmailRepository repository,
        ILogger<AuthorizedEmailService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AuthorizedEmailResponseDto> CreateAuthorizedEmailAsync(CreateAuthorizedEmailRequest request)
    {
        // Vérifier si l'email existe déjà
        var existing = await _repository.GetByEmailAsync(request.Email);
        if (existing != null)
        {
            throw new InvalidOperationException($"L'email {request.Email} existe déjà dans la liste des emails autorisés");
        }

        // Créer le nouvel email autorisé
        var authorizedEmail = new AuthorizedEmail
        {
            Email = request.Email,
            IsImported = request.IsImported,
            SyncDate = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(authorizedEmail);

        return new AuthorizedEmailResponseDto
        {
            SyncId = created.SyncId,
            Email = created.Email,
            IsImported = created.IsImported,
            SyncDate = created.SyncDate
        };
    }

    public async Task<AuthorizedEmailCheckResponseDto> CheckEmailExistsAsync(CheckEmailExistsRequest request)
    {
        var emailRecord = await _repository.GetByEmailAsync(request.Email);
        return new AuthorizedEmailCheckResponseDto
        {
            Exists = emailRecord != null,
            Email = request.Email
        };
    }

    public async Task<AuthorizedEmailStatusUpdateResponseDto> ActivateEmailAsync(UpdateEmailStatusRequest request)
    {
        var emailRecord = await _repository.GetByEmailAsync(request.Email);
        if (emailRecord == null)
        {
            return new AuthorizedEmailStatusUpdateResponseDto
            {
                Success = false,
                Message = $"Email {request.Email} non trouvé",
                Email = request.Email,
                IsImported = false
            };
        }

        emailRecord.IsImported = true;
        emailRecord.SyncDate = DateTime.UtcNow;
        await _repository.UpdateAsync(emailRecord);

        return new AuthorizedEmailStatusUpdateResponseDto
        {
            Success = true,
            Message = $"Email {request.Email} activé avec succès",
            Email = request.Email,
            IsImported = true
        };
    }

    public async Task<AuthorizedEmailStatusUpdateResponseDto> DeactivateEmailAsync(UpdateEmailStatusRequest request)
    {
        var emailRecord = await _repository.GetByEmailAsync(request.Email);
        if (emailRecord == null)
        {
            return new AuthorizedEmailStatusUpdateResponseDto
            {
                Success = false,
                Message = $"Email {request.Email} non trouvé",
                Email = request.Email,
                IsImported = false
            };
        }

        emailRecord.IsImported = false;
        emailRecord.SyncDate = DateTime.UtcNow;
        await _repository.UpdateAsync(emailRecord);

        return new AuthorizedEmailStatusUpdateResponseDto
        {
            Success = true,
            Message = $"Email {request.Email} désactivé avec succès",
            Email = request.Email,
            IsImported = false
        };
    }

    public async Task<bool> DeleteEmailAsync(DeleteEmailRequest request)
    {
        return await _repository.DeleteByEmailAsync(request.Email);
    }

    public async Task<IEnumerable<AuthorizedEmailResponseDto>> GetAllAsync()
    {
        var emails = await _repository.GetAllAsync();
        return emails.Select(e => new AuthorizedEmailResponseDto
        {
            SyncId = e.SyncId,
            Email = e.Email,
            IsImported = e.IsImported,
            SyncDate = e.SyncDate
        });
    }

    public async Task<IEnumerable<AuthorizedEmailQueryResult>> SearchAsync(GetAuthorizedEmailQuery query)
    {
        var emails = await _repository.SearchAsync(query.Email, query.IsImported);
        return emails.Select(e => new AuthorizedEmailQueryResult
        {
            SyncId = e.SyncId,
            Email = e.Email,
            IsImported = e.IsImported,
            SyncDate = e.SyncDate
        });
    }
}