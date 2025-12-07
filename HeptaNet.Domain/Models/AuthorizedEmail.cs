using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Models;

public class AuthorizedEmail
{
    public int SyncId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public bool IsImported { get; set; } = false;

    public DateTime? SyncDate { get; set; }
}