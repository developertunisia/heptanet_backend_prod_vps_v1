using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HeptaNet.API.Services;
using HeptaNet.Domain.Dto;
using HeptaNet.Domain.Request;

namespace HeptaNet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;
    
    public ConversationsController(
        IConversationService conversationService,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
    
    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserConversations([FromQuery] bool includeArchived = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var conversations = await _conversationService.GetUserConversationsAsync(userId, includeArchived);
            
            return Ok(new
            {
                success = true,
                data = conversations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations");
            return StatusCode(500, new { success = false, message = "Failed to retrieve conversations" });
        }
    }
    
    /// <summary>
    /// Get a specific conversation by ID
    /// </summary>
    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var conversation = await _conversationService.GetConversationDtoByIdAsync(conversationId, userId);
            
            if (conversation == null)
            {
                return NotFound(new { success = false, message = "Conversation not found or access denied" });
            }
            
            return Ok(new
            {
                success = true,
                data = conversation
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation");
            return StatusCode(500, new { success = false, message = "Failed to retrieve conversation" });
        }
    }
    
    /// <summary>
    /// Create or get a private conversation with another user
    /// </summary>
    [HttpPost("private")]
    public async Task<IActionResult> CreatePrivateConversation([FromBody] CreatePrivateConversationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var conversation = await _conversationService.CreateOrGetPrivateConversationAsync(userId, request.OtherUserId);
            
            // Map to DTO to avoid circular references
            var conversationDto = await _conversationService.GetConversationDtoByIdAsync(conversation.ConversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation created or retrieved successfully",
                data = conversationDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating private conversation");
            return StatusCode(500, new { success = false, message = "Failed to create conversation" });
        }
    }
    
    /// <summary>
    /// Create a group conversation
    /// </summary>
    [HttpPost("group")]
    public async Task<IActionResult> CreateGroupConversation([FromBody] CreateGroupConversationRequest request)
    {
        try
        {
            var conversation = await _conversationService.CreateGroupConversationAsync(request.GroupId);
            
            // Map to DTO to avoid circular references
            // Use current user's ID to get their view of the conversation
            var conversationDto = await _conversationService.GetConversationDtoByIdAsync(conversation.ConversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Group conversation created successfully",
                data = conversationDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group conversation");
            return StatusCode(500, new { success = false, message = "Failed to create group conversation" });
        }
    }
    
    /// <summary>
    /// Archive a conversation
    /// </summary>
    [HttpPost("{conversationId}/archive")]
    public async Task<IActionResult> ArchiveConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.ArchiveConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation archived successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving conversation");
            return StatusCode(500, new { success = false, message = "Failed to archive conversation" });
        }
    }
    
    /// <summary>
    /// Unarchive a conversation
    /// </summary>
    [HttpPost("{conversationId}/unarchive")]
    public async Task<IActionResult> UnarchiveConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.UnarchiveConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation unarchived successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unarchiving conversation");
            return StatusCode(500, new { success = false, message = "Failed to unarchive conversation" });
        }
    }
    
    /// <summary>
    /// Pin a conversation
    /// </summary>
    [HttpPost("{conversationId}/pin")]
    public async Task<IActionResult> PinConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.PinConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation pinned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pinning conversation");
            return StatusCode(500, new { success = false, message = "Failed to pin conversation" });
        }
    }
    
    /// <summary>
    /// Unpin a conversation
    /// </summary>
    [HttpPost("{conversationId}/unpin")]
    public async Task<IActionResult> UnpinConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.UnpinConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation unpinned successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpinning conversation");
            return StatusCode(500, new { success = false, message = "Failed to unpin conversation" });
        }
    }
    
    /// <summary>
    /// Mute a conversation
    /// </summary>
    [HttpPost("{conversationId}/mute")]
    public async Task<IActionResult> MuteConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.MuteConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation muted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error muting conversation");
            return StatusCode(500, new { success = false, message = "Failed to mute conversation" });
        }
    }
    
    /// <summary>
    /// Unmute a conversation
    /// </summary>
    [HttpPost("{conversationId}/unmute")]
    public async Task<IActionResult> UnmuteConversation(int conversationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _conversationService.UnmuteConversationAsync(conversationId, userId);
            
            return Ok(new
            {
                success = true,
                message = "Conversation unmuted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmuting conversation");
            return StatusCode(500, new { success = false, message = "Failed to unmute conversation" });
        }
    }
}


