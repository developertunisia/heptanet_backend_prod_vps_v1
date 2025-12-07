using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models;
namespace HeptaNet.Domain.Dto;
public class CreateGroupDto
{
    [Required(ErrorMessage = "Le nom du groupe est requis")]
    [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
    public string? Description { get; set; }
}

public class UpdateGroupDto
{
    [Required(ErrorMessage = "Le nom du groupe est requis")]
    [StringLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
    public string? Description { get; set; }
}

public class GroupResponseDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public class GroupDetailDto
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedBy { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<GroupMemberResponseDto> Members { get; set; } = new();
}