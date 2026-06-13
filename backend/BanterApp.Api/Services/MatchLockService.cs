using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public static class MatchLockService
{
    private static readonly HashSet<string> LockedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "FT", "LIVE", "HT", "1H", "2H", "AET", "PEN", "finished", "live"
    };

    public static bool IsLocked(Match match, DateTimeOffset? now = null)
    {
        var utcNow = now ?? DateTimeOffset.UtcNow;
        if (LockedStatuses.Contains(match.Status))
        {
            return true;
        }

        return match.KickoffTime <= utcNow;
    }

    public static string LockReason(Match match, DateTimeOffset? now = null) =>
        IsLocked(match, now)
            ? "Predictions are locked once the match kicks off."
            : string.Empty;
}
