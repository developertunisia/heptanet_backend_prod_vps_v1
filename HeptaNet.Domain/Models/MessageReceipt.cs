using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public enum ReceiptStatus
{
    Sent = 0,
    Delivered = 1,
    Read = 2,
    Failed = 3
}

public class MessageReceipt
{
    public int ReceiptId { get; set; }
    
    public int MessageId { get; set; }
    public int UserId { get; set; }
    
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Sent;
    
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Message Message { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}


