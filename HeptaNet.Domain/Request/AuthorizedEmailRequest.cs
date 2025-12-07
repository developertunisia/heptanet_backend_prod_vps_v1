using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Request;

public record CreateAuthorizedEmailRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
    public string Email { get; init; } = string.Empty;

    public bool IsImported { get; init; } = false;
}

public record CheckEmailExistsRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; init; } = string.Empty;
}

public record UpdateEmailStatusRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Le statut est requis")]
    public bool IsImported { get; init; }
}

public record DeleteEmailRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; init; } = string.Empty;
}