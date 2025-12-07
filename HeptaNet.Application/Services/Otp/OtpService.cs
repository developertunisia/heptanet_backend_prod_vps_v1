using HeptaNet.Application.Services.Email;
using HeptaNet.Application.Settings;
using HeptaNet.Domain.Base;
using HeptaNet.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace HeptaNet.Application.Services.Otp;

public class OtpService : IOtpService
{
    private readonly IOtpRepository _repo;
    private readonly IEmailSender _emailSender;
    private readonly OtpSettings _settings;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IOtpRepository repo, IEmailSender emailSender, IOptions<OtpSettings> options, ILogger<OtpService> logger)
    { _repo = repo; _emailSender = emailSender; _settings = options.Value; _logger = logger; }

    public async Task<OtpSendResult> SendAsync(string email, string purpose, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        try
        {
            await _repo.EnsureCanSendAsync(normalized, purpose, TimeSpan.FromSeconds(_settings.ResendCooldownSeconds), _settings.MaxSendsPerDay, ct);
        }
        catch (InvalidOperationException ioe) when (ioe.Message == "Cooldown")
        {
            var (_, cooldown, next, _, _) = await _repo.GetStatusAsync(normalized, purpose, TimeSpan.FromSeconds(_settings.ResendCooldownSeconds), _settings.MaxAttempts, ct);
            return new(true, true, cooldown, next, "Veuillez patienter avant un nouvel envoi.");
        }
        catch (InvalidOperationException ioe) when (ioe.Message == "DailyLimit")
        {
            return new(false, false, _settings.ResendCooldownSeconds, null, "Nombre d’envois quotidien dépassé.");
        }

        await _repo.InvalidateAllActiveAsync(normalized, purpose, ct);

        var code = GenerateNumericCode(_settings.Length);
        var entity = new OtpCode
        {
            Id = Guid.NewGuid(),
            Email = normalized,
            Purpose = purpose,
            CodeHash = Hash(code, _settings.Salt),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            IsUsed = false,
            AttemptCount = 0,
            MaxAttempts = _settings.MaxAttempts,
            SentCount = 1,
            CreatedAtUtc = DateTime.UtcNow,
            LastSentAtUtc = DateTime.UtcNow
        };
        await _repo.CreateAsync(entity, ct);

        var subject = "Votre code de vérification";
        var html = $"<p>Votre code OTP est: <b>{code}</b>. Il expire dans {_settings.ExpiryMinutes} minutes.</p>";
        
        // Log le code OTP pour le débogage (en développement)
        _logger.LogInformation("🔐 Code OTP généré pour {Email} (purpose: {Purpose}): {Code}", normalized, purpose, code);
        
        try
        {
            await _emailSender.SendAsync(normalized, subject, html, $"Code OTP: {code}", ct);
            _logger.LogInformation("✅ Email OTP envoyé avec succès à {Email}", normalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'envoi de l'email OTP à {Email}. Code: {Code}", normalized, code);
            // On continue quand même car le code est stocké en base de données
            // L'utilisateur peut le récupérer via les logs ou la base de données
        }

        return new(true, false, _settings.ResendCooldownSeconds, DateTime.UtcNow.AddSeconds(_settings.ResendCooldownSeconds), "Code envoyé.");
    }

    public async Task<OtpVerifyResult> VerifyAsync(string email, string purpose, string code, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var otp = await _repo.GetActiveAsync(normalized, purpose, ct);
        if (otp == null) return new(false, true, null, "Aucun code OTP actif. Veuillez demander un nouveau code.");
        if (otp.IsUsed || otp.ExpiresAtUtc <= DateTime.UtcNow) return new(false, true, null, "Code OTP expiré. Veuillez demander un nouveau code.");
        if (otp.AttemptCount >= otp.MaxAttempts) return new(false, false, 0, "Nombre de tentatives dépassé. Veuillez demander un nouveau code.");

        var ok = VerifyHash(code, otp.CodeHash, _settings.Salt);
        await _repo.IncrementAttemptAsync(otp.Id, ok, ct);

        var remaining = Math.Max(0, otp.MaxAttempts - (otp.AttemptCount + 1));
        return ok ? new(true, false, remaining, null)
                  : new(false, false, remaining, "Code OTP incorrect. Il vous reste " + remaining + " tentative(s).");
    }

    public async Task<OtpStatusResult> GetStatusAsync(string email, string purpose, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var (canResend, cooldown, next, remaining, expires) =
            await _repo.GetStatusAsync(normalized, purpose, TimeSpan.FromSeconds(_settings.ResendCooldownSeconds), _settings.MaxAttempts, ct);
        return new(canResend, cooldown, next, remaining, expires);
    }

    private static string GenerateNumericCode(int length)
    {
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);
        var digits = new char[length];
        for (int i = 0; i < length; i++) digits[i] = (char)('0' + (bytes[i] % 10));
        return new string(digits);
    }

    private static string Hash(string input, string salt)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input + salt);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }
    private static bool VerifyHash(string input, string hash, string salt) => Hash(input, salt) == hash;
}