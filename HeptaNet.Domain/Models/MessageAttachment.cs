using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class MessageAttachment
{
    public int AttachmentId { get; set; }
    
    public int MessageId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    public string FileUrl { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    // For images/videos
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationSeconds { get; set; }
    
    // Thumbnail for media
    public string? ThumbnailUrl { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Message Message { get; set; } = null!;
}


