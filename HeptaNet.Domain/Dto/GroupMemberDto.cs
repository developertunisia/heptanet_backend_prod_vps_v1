using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models;
namespace HeptaNet.Domain.Dto;

public class AddGroupMemberDto
{
    [Required(ErrorMessage = "L'ID du groupe est requis")]
    public int GroupId { get; set; }
    
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    public int UserId { get; set; }
}

public class RemoveGroupMemberDto
{
    [Required(ErrorMessage = "L'ID du groupe est requis")]
    public int GroupId { get; set; }
    
    [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
    public int UserId { get; set; }
}

public class GroupMemberResponseDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
