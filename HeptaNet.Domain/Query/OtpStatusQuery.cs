// Optionnel: pour récupérer l'état (cooldown / expiry / tentatives restantes)
public class OtpStatusQueryDto
{
    public string Email { get; set; } = default!;
    public string? Purpose { get; set; } = "register";
}