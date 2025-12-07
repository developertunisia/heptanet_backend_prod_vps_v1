using System.ComponentModel.DataAnnotations;
using HeptaNet.Domain.Models; 
namespace HeptaNet.Domain.Dto;

public class BlacklistLogResponseDto
{
    public int LogId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ActionBy { get; set; }
    public string ActionByName { get; set; } = string.Empty;
    public BlacklistActionType ActionType { get; set; }
    public DateTime DateAction { get; set; }
    public string? Reason { get; set; }
}
