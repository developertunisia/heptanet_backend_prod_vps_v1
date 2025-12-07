using Microsoft.AspNetCore.Mvc;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Request;
using HeptaNet.Domain.Query;
using HeptaNet.Application.Services;
using Microsoft.AspNetCore.Authorization;
using HeptaNet.Domain.Constants;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class AuthorizedEmailController : ControllerBase
{
    private readonly IAuthorizedEmailService _authorizedEmailService;
    private readonly ILogger<AuthorizedEmailController> _logger;

    public AuthorizedEmailController(
        IAuthorizedEmailService authorizedEmailService,
        ILogger<AuthorizedEmailController> logger)
    {
        _authorizedEmailService = authorizedEmailService;
        _logger = logger;
    }


    /// <summary>
    /// Ajoute un nouvel email à la liste des emails autorisés
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ManageAuthorizedEmails")]
    public async Task<ActionResult<AuthorizedEmailResponseDto>> CreateAuthorizedEmail(
        [FromBody] CreateAuthorizedEmailRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authorizedEmailService.CreateAuthorizedEmailAsync(request);

            return CreatedAtAction(
                nameof(CheckEmail),
                new { email = result.Email },
                result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'email autorisé {Email}", request.Email);
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Vérifie si un email existe dans AuthorizedEmails
    /// </summary>
    [HttpGet("check/{email}")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthorizedEmailCheckResponseDto>> CheckEmail(string email)
    {
        try
        {
            var request = new CheckEmailExistsRequest { Email = email };
            var result = await _authorizedEmailService.CheckEmailExistsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'email {Email}", email);
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Active un email (IsImported = true)
    /// </summary>
    [HttpPut("activate")]
    [Authorize(Policy = "ManageAuthorizedEmails")]
    public async Task<ActionResult<AuthorizedEmailStatusUpdateResponseDto>> ActivateEmail(
        [FromBody] UpdateEmailStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authorizedEmailService.ActivateEmailAsync(request);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'activation de l'email {Email}", request.Email);
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Désactive un email (IsImported = false)
    /// </summary>
    [HttpPut("deactivate")]
    public async Task<ActionResult<AuthorizedEmailStatusUpdateResponseDto>> DeactivateEmail(
        [FromBody] UpdateEmailStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authorizedEmailService.DeactivateEmailAsync(request);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la désactivation de l'email {Email}", request.Email);
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Supprime un email de AuthorizedEmails
    /// </summary>
    [HttpDelete("{email}")]
    [Authorize(Policy = "ManageAuthorizedEmails")] 
    public async Task<ActionResult> DeleteEmail(string email)
    {
        try
        {
            var request = new DeleteEmailRequest { Email = email };
            var result = await _authorizedEmailService.DeleteEmailAsync(request);

            if (!result)
            {
                return NotFound(new { message = $"Email {email} non trouvé" });
            }

            return Ok(new { message = $"Email {email} supprimé avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la suppression de l'email {Email}", email);
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Retourne tous les emails de AuthorizedEmails
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ViewAuthorizedEmails")]
    public async Task<ActionResult<IEnumerable<AuthorizedEmailResponseDto>>> GetAllAuthorizedEmails()
    {
        try
        {
            var emails = await _authorizedEmailService.GetAllAsync();
            return Ok(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des données AuthorizedEmail");
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }

    /// <summary>
    /// Recherche et filtre les emails autorisés
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AuthorizedEmailQueryResult>>> SearchEmails(
        [FromQuery] GetAuthorizedEmailQuery query)
    {
        try
        {
            var results = await _authorizedEmailService.SearchAsync(query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la recherche des emails");
            return StatusCode(500, new { message = "Une erreur interne s'est produite" });
        }
    }
}