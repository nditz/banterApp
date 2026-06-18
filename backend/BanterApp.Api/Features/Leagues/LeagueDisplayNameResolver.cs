using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;

namespace BanterApp.Api.Features.Leagues;

public static class LeagueDisplayNameResolver
{
    /// <summary>Resolve a league player label — email for registered users, guest id for anonymous.</summary>
    public static async Task<string> ResolveAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (user.IsAuthenticated && user.UserId.HasValue)
        {
            var registered = await db.Users.FindAsync([user.UserId.Value], ct);
            if (!string.IsNullOrWhiteSpace(registered?.Email))
            {
                return Truncate(registered.Email.Trim());
            }
        }

        if (user.AnonymousUserId.HasValue)
        {
            return BuildGuestName(user.AnonymousUserId.Value);
        }

        return "Player";
    }

    public static string BuildGuestName(Guid anonymousUserId) =>
        $"Guest-{anonymousUserId.ToString()[..4].ToUpperInvariant()}";

    public static string EnsureUniqueInLeague(string baseName, IEnumerable<string> takenNames)
    {
        var taken = new HashSet<string>(takenNames, StringComparer.OrdinalIgnoreCase);
        if (!taken.Contains(baseName))
        {
            return baseName;
        }

        for (var i = 2; i < 100; i++)
        {
            var suffix = $" ({i})";
            var maxBase = StringLimits.LeagueMemberDisplayName - suffix.Length;
            var trimmedBase = baseName.Length > maxBase ? baseName[..maxBase] : baseName;
            var candidate = $"{trimmedBase}{suffix}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{Truncate(baseName[..Math.Max(1, StringLimits.LeagueMemberDisplayName - 5)])}-{Guid.NewGuid().ToString()[..4]}";
    }

    private static string Truncate(string value) =>
        StringLimits.Truncate(value, StringLimits.LeagueMemberDisplayName) ?? value;
}
