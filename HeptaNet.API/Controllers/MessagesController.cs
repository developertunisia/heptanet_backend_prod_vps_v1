using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Security.Claims;
using HeptaNet.API.Services;
using HeptaNet.API.Requests;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Models;
using HeptaNet.Domain.Request;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<MessagesController> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public MessagesController(
        IMessageService messageService,
        IConversationService conversationService,
        ILogger<MessagesController> logger,
        IWebHostEnvironment environment)
    {
        _messageService = messageService;
        _conversationService = conversationService;
        _logger = logger;
        _environment = environment;
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
    
    /// <summary>
    /// Send a new message
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var messageDto = await _messageService.SendMessageAsync(dto, userId);
            
            return Ok(new
            {
                success = true,
                message = "Message sent successfully",
                data = messageDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { success = false, message = "Failed to send message" });
        }
    }
    
    /// <summary>
    /// Send a voice message (multipart/form-data)
    /// </summary>
    [HttpPost("voice")]
    [RequestSizeLimit(10_000_000)] // 10 MB max
    public async Task<IActionResult> SendVoiceMessage([FromForm] SendVoiceMessageRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Valider le fichier
            if (request.AudioFile == null || request.AudioFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "Audio file is required" });
            }
            
            // Valider la taille (max 10 MB)
            if (request.AudioFile.Length > 10_000_000)
            {
                return BadRequest(new { success = false, message = "File size exceeds 10 MB limit" });
            }
            
            // Valider le type de fichier
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" };
            var extension = Path.GetExtension(request.AudioFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Invalid file type. Allowed: mp3, wav, ogg, m4a, aac" 
                });
            }
            
            // Créer le DTO pour MessageService
            var sendMessageDto = new SendMessageDto
            {
                ConversationId = request.ConversationId,
                ReceiverId = request.ReceiverId,
                GroupId = request.GroupId,
                Type = MessageType.Audio,
                Content = "🎤 Message vocal", // Sera mis à jour avec la durée
                ReplyToMessageId = request.ReplyToMessageId
            };
            
            // Envoyer le message avec le fichier
            var message = await _messageService.SendMessageAsync(
                sendMessageDto, 
                userId, 
                request.AudioFile);
            
            return Ok(new
            {
                success = true,
                message = "Voice message sent successfully",
                data = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending voice message");
            return StatusCode(500, new { success = false, message = "Failed to send voice message" });
        }
    }
    
    /// <summary>
    /// Get messages for a conversation with pagination (includes attachments with full URLs)
    /// </summary>
    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversationMessages(
        int conversationId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Verify user is participant of this conversation
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null)
            {
                return NotFound(new { success = false, message = "Conversation not found or access denied" });
            }
            
            // Construire l'URL de base
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            
            // Récupérer les messages avec attachments et URLs complètes
            var messages = await _messageService.GetConversationMessagesWithAttachmentsAsync(
                conversationId, 
                userId, 
                baseUrl,
                page, 
                pageSize);
            
            return Ok(new
            {
                success = true,
                data = messages,
                page,
                pageSize,
                total = messages.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return StatusCode(500, new { success = false, message = "Failed to retrieve messages" });
        }
    }
    
    /// <summary>
    /// Get a specific message by ID (with attachments and full URLs)
    /// </summary>
    [HttpGet("{messageId}")]
    public async Task<IActionResult> GetMessage(int messageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var messageDto = await _messageService.GetMessageByIdAsync(messageId);
            
            if (messageDto == null)
            {
                return NotFound(new { success = false, message = "Message not found" });
            }
            
            // Vérifier les permissions
            var conversation = await _conversationService.GetConversationByIdAsync(messageDto.ConversationId, userId);
            if (conversation == null)
            {
                return Forbid();
            }
            
            // Construire l'URL de base et mettre à jour les URLs complètes
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            foreach (var attachment in messageDto.Attachments)
            {
                attachment.FullFileUrl = $"{baseUrl.TrimEnd('/')}{attachment.FileUrl}";
            }
            
            return Ok(new
            {
                success = true,
                data = messageDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message");
            return StatusCode(500, new { success = false, message = "Failed to retrieve message" });
        }
    }
    
    /// <summary>
    /// Get voice message audio file
    /// </summary>
    [HttpGet("{messageId}/voice")]
    public async Task<IActionResult> GetVoiceMessage(int messageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Récupérer le message avec ses attachments
            var messageDto = await _messageService.GetMessageByIdAsync(messageId);
            
            if (messageDto == null)
            {
                return NotFound(new { success = false, message = "Message not found" });
            }
            
            // Vérifier que l'utilisateur a accès à cette conversation
            var conversation = await _conversationService.GetConversationByIdAsync(
                messageDto.ConversationId, 
                userId);
            
            if (conversation == null)
            {
                return Forbid();
            }
            
            // Vérifier que c'est un message vocal
            if (messageDto.Type != MessageType.Audio)
            {
                return BadRequest(new { success = false, message = "This message is not a voice message" });
            }
            
            // Récupérer l'attachment audio
            var audioAttachment = messageDto.Attachments
                .FirstOrDefault(a => a.ContentType.StartsWith("audio/"));
            
            if (audioAttachment == null)
            {
                return NotFound(new { success = false, message = "Audio file not found for this message" });
            }
            
            // Construire le chemin complet du fichier
            // Utiliser la même logique que LocalFileStorageService pour la cohérence
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isProduction = !string.IsNullOrEmpty(env) && 
                             (env.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
                              env.Equals("Prod", StringComparison.OrdinalIgnoreCase));
            
            // Vérifier aussi si on est dans Docker (fichier /.dockerenv existe)
            var isDocker = System.IO.File.Exists("/.dockerenv");
            
            string basePath;
            if (isProduction || isDocker)
            {
                // En production Docker, utiliser toujours /app/uploads (volume monté)
                basePath = "/app/uploads";
                _logger.LogInformation("Production/Docker mode: Using /app/uploads for file retrieval");
            }
            else
            {
                // En développement, utiliser le chemin relatif
                basePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
                _logger.LogInformation($"Development mode: Using {basePath} for file retrieval");
            }
            
            // Enlever le préfixe /uploads/ et construire le chemin relatif
            var relativePath = audioAttachment.FileUrl
                .Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase);
            var safePath = Path.Combine(relativePath.Split('/'));
            var webRootPath = Path.Combine(basePath, safePath);
            
            // Vérifier que le fichier existe
            if (!System.IO.File.Exists(webRootPath))
            {
                return NotFound(new { success = false, message = "Audio file not found on server" });
            }
            
            // Lire le fichier
            var fileBytes = await System.IO.File.ReadAllBytesAsync(webRootPath);
            var contentType = audioAttachment.ContentType;
            
            // Retourner le fichier
            return File(fileBytes, contentType, audioAttachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving voice message");
            return StatusCode(500, new { success = false, message = "Failed to retrieve voice message" });
        }
    }
    
    /// <summary>
    /// Mark a message as read
    /// </summary>
    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _messageService.MarkAsReadAsync(messageId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Message marked as read"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read");
            return StatusCode(500, new { success = false, message = "Failed to mark message as read" });
        }
    }
    
    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    [HttpPost("conversation/{conversationId}/read-all")]
    public async Task<IActionResult> MarkConversationAsRead(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _messageService.MarkConversationAsReadAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "All messages marked as read"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking conversation as read");
            return StatusCode(500, new { success = false, message = "Failed to mark conversation as read" });
        }
    }
    
    /// <summary>
    /// Edit a message
    /// </summary>
    [HttpPut("{messageId}")]
    public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var message = await _messageService.EditMessageAsync(messageId, request.Content, userId);
            
            return Ok(new
            {
                success = true,
                message = "Message edited successfully",
                data = message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message");
            return StatusCode(500, new { success = false, message = "Failed to edit message" });
        }
    }
    
    /// <summary>
    /// Delete a message
    /// </summary>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _messageService.DeleteMessageAsync(messageId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Message deleted successfully"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message");
            return StatusCode(500, new { success = false, message = "Failed to delete message" });
        }
    }
}


