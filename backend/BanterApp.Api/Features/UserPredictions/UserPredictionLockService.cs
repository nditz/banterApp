using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.UserPredictions;

public sealed class UserPredictionLockService(
    AppDbContext db,
    IOptions<FootballReferenceDataOptions> options)
{
    public async Task<bool> IsLockedAsync(CancellationToken cancellationToken = default)
    {
        await LockPredictionsIfNeededAsync(cancellationToken);
        return await db.UserPredictions.AnyAsync(p => p.IsLocked, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLockDeadlineAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value.PredictionLockDeadline;
        var firstKickoff = await db.Matches
            .AsNoTracking()
            .Where(m => m.KickoffTime > DateTimeOffset.UtcNow)
            .OrderBy(m => m.KickoffTime)
            .Select(m => (DateTimeOffset?)m.KickoffTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (configured is null)
        {
            return firstKickoff;
        }

        return firstKickoff is null || configured < firstKickoff
            ? configured
            : firstKickoff;
    }

    public async Task LockPredictionsIfNeededAsync(CancellationToken cancellationToken = default)
    {
        var deadline = await GetEffectiveLockTimeAsync(cancellationToken);
        if (deadline is null || deadline > DateTimeOffset.UtcNow)
        {
            return;
        }

        var unlocked = await db.UserPredictions
            .Where(p => !p.IsLocked)
            .ToListAsync(cancellationToken);

        if (unlocked.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var prediction in unlocked)
        {
            prediction.IsLocked = true;
            prediction.LockedAt = now;
            prediction.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<DateTimeOffset?> GetEffectiveLockTimeAsync(CancellationToken cancellationToken)
    {
        var configured = options.Value.PredictionLockDeadline;
        var firstKickoff = await db.Matches
            .AsNoTracking()
            .OrderBy(m => m.KickoffTime)
            .Select(m => (DateTimeOffset?)m.KickoffTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (configured is null)
        {
            return firstKickoff;
        }

        if (firstKickoff is null)
        {
            return configured;
        }

        return configured < firstKickoff ? configured : firstKickoff;
    }
}
