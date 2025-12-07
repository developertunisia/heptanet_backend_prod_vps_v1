using HeptaNet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeptaNet.Domain.Base
{
    public interface IOtpRepository
    {
        Task<OtpCode?> GetActiveAsync(string email, string purpose, CancellationToken ct);
        Task InvalidateAllActiveAsync(string email, string purpose, CancellationToken ct);
        Task CreateAsync(OtpCode entity, CancellationToken ct);
        Task IncrementAttemptAsync(Guid id, bool success, CancellationToken ct);
        Task EnsureCanSendAsync(string email, string purpose, TimeSpan cooldown, int maxSendsPerDay, CancellationToken ct);
        Task<(bool canResend, int cooldownSeconds, DateTime? nextAllowedAtUtc, int? remainingAttempts, DateTime? expiresAtUtc)> GetStatusAsync(string email, string purpose, TimeSpan cooldown, int maxAttempts, CancellationToken ct);
    }
}
