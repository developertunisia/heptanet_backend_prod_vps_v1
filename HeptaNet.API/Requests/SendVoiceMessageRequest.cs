using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HeptaNet.API.Requests;

public class SendVoiceMessageRequest
{
    [Required(ErrorMessage = "L'ID de la conversation est requis")]
    public int ConversationId { get; set; }
    
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public int? ReplyToMessageId { get; set; }
    
    [Required(ErrorMessage = "Le fichier audio est requis")]
    public IFormFile AudioFile { get; set; } = null!;
}

