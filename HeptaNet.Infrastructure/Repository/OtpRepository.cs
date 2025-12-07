using HeptaNet.Domain.Base;
using HeptaNet.Domain.Models;
using HeptaNet.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HeptaNet.Infrastructure.Repository;

public class OtpRepository : IOtpRepository
{
    private readonly MyAppContext _db;
    public OtpRepository(MyAppContext db) { _db = db; }

    public async Task<OtpCode?> GetActiveAsync(string email, string purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.OtpCodes
            .Where(x => x.Email == email && x.Purpose == purpose && !x.IsUsed && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task InvalidateAllActiveAsync(string email, string purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var list = await _db.OtpCodes
            .Where(x => x.Email == email && x.Purpose == purpose && !x.IsUsed && x.ExpiresAtUtc > now)
            .ToListAsync(ct);
        foreach (var item in list) item.IsUsed = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateAsync(OtpCode entity, CancellationToken ct)
    {
        _db.OtpCodes.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementAttemptAsync(Guid id, bool success, CancellationToken ct)
    {
        var otp = await _db.OtpCodes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (otp == null) return;
        otp.AttemptCount += 1;
        if (success) otp.IsUsed = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task EnsureCanSendAsync(string email, string purpose, TimeSpan cooldown, int maxSendsPerDay, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var last = await _db.OtpCodes
            .Where(x => x.Email == email && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (last != null && last.LastSentAtUtc.HasValue && (now - last.LastSentAtUtc.Value) < cooldown)
            throw new InvalidOperationException("Cooldown");

        var since = now.Date;
        var sentToday = await _db.OtpCodes.CountAsync(x => x.Email == email && x.Purpose == purpose && x.CreatedAtUtc >= since, ct);
        if (sentToday >= maxSendsPerDay) throw new InvalidOperationException("DailyLimit");
    }

    public async Task<(bool, int, DateTime?, int?, DateTime?)> GetStatusAsync(string email, string purpose, TimeSpan cooldown, int maxAttempts, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var last = await _db.OtpCodes
            .Where(x => x.Email == email && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var canResend = true;
        int cooldownSec = 0;
        DateTime? nextAllowed = null;

        if (last?.LastSentAtUtc is DateTime sentAt)
        {
            var diff = (int)(cooldown - (now - sentAt)).TotalSeconds;
            if (diff > 0) { canResend = false; cooldownSec = diff; nextAllowed = sentAt.Add(cooldown); }
        }

        var active = await GetActiveAsync(email, purpose, ct);
        int? remaining = active is null ? null : Math.Max(0, active.MaxAttempts - active.AttemptCount);
        DateTime? expiresAt = active?.ExpiresAtUtc;

        return (canResend, Math.Max(0, cooldownSec), nextAllowed, remaining, expiresAt);
    }
}