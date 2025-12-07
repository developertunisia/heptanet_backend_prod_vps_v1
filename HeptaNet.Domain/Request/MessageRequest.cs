using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Request;

public record EditMessageRequest
{
    [Required(ErrorMessage = "Le contenu du message est requis")]
    public string Content { get; init; } = string.Empty;
}

public record CreatePrivateConversationRequest
{
    [Required(ErrorMessage = "L'ID de l'autre utilisateur est requis")]
    public int OtherUserId { get; init; }
}

public record CreateGroupConversationRequest
{
    [Required(ErrorMessage = "L'ID du groupe est requis")]
    public int GroupId { get; init; }
}

