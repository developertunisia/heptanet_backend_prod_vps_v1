public class SendOtpResponseDto
{
    public string Status { get; set; } = "sent"; // "sent" | "cooldown" | "limit"
    public int CooldownSeconds { get; set; } = 60; // si cooldown appliqué côté serveur
    public DateTime? NextAllowedAtUtc { get; set; } // si calculé côté serveur
    public string? Message { get; set; }
}

public class VerifyOtpResponseDto
{
    public bool Valid { get; set; }
    public bool Expired { get; set; }
    public int? RemainingAttempts { get; set; } // si fourni
    public string? Message { get; set; }
}

// Optionnel: pour afficher un timer et des infos UX côté frontend sans renvoyer de code
public class OtpStatusResponseDto
{
    public bool CanResend { get; set; }
    public int CooldownSeconds { get; set; }
    public DateTime? NextAllowedAtUtc { get; set; }
    public int? RemainingAttempts { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}