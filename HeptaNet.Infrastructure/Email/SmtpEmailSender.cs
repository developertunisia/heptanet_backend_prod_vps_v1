using HeptaNet.Application.Services.Email;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using HeptaNet.Application.Settings;
using Microsoft.Extensions.Logging;

namespace HeptaNet.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("📧 Tentative d'envoi d'email à {ToEmail} via {Host}:{Port}", toEmail, _settings.Host, _settings.Port);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_settings.User, _settings.Password),
                Timeout = 30000 // 30 secondes
            };
            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.From, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);
            if (!string.IsNullOrWhiteSpace(plainTextBody))
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));

            await client.SendMailAsync(msg, ct);
            _logger.LogInformation("✅ Email envoyé avec succès à {ToEmail}", toEmail);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "❌ Erreur SMTP lors de l'envoi à {ToEmail}: {Message}", toEmail, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors de l'envoi d'email à {ToEmail}: {Message}", toEmail, ex.Message);
            throw;
        }
    }
}