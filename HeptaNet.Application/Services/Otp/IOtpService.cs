namespace HeptaNet.Application.Services.Otp;

public record OtpSendResult(bool Success, bool IsCooldown, int CooldownSeconds, DateTime? NextAllowedAtUtc, string? Message);
public record OtpVerifyResult(bool Valid, bool Expired, int? RemainingAttempts, string? Message);
public record OtpStatusResult(bool CanResend, int CooldownSeconds, DateTime? NextAllowedAtUtc, int? RemainingAttempts, DateTime? ExpiresAtUtc);

public interface IOtpService
{
    Task<OtpSendResult> SendAsync(string email, string purpose, CancellationToken ct = default);
    Task<OtpVerifyResult> VerifyAsync(string email, string purpose, string code, CancellationToken ct = default);
    Task<OtpStatusResult> GetStatusAsync(string email, string purpose, CancellationToken ct = default);
}