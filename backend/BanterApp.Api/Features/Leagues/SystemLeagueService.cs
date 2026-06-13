using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Leagues;

public static class SystemLeagueService
{
    private static readonly Dictionary<string, string> CountryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GB"] = "United Kingdom",
        ["US"] = "United States",
        ["NG"] = "Nigeria",
        ["ZA"] = "South Africa",
        ["KE"] = "Kenya",
        ["GH"] = "Ghana",
        ["IN"] = "India",
        ["AU"] = "Australia",
        ["CA"] = "Canada",
        ["DE"] = "Germany",
        ["FR"] = "France",
        ["ES"] = "Spain",
        ["IT"] = "Italy",
        ["BR"] = "Brazil",
        ["AR"] = "Argentina",
        ["MX"] = "Mexico",
        ["JP"] = "Japan",
        ["KR"] = "South Korea",
        ["SA"] = "Saudi Arabia",
        ["AE"] = "UAE",
    };

    public static string NormalizeCountryCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 2)
        {
            return "GB";
        }

        return code.Trim().ToUpperInvariant();
    }

    public static string CountryLeagueName(string countryCode) =>
        CountryNames.TryGetValue(countryCode, out var name)
            ? $"{name} Fans"
            : $"{countryCode} Fans";

    /// <summary>
    /// Ensures global + country system leagues exist and the user is enrolled.
    /// Persists browser-detected country on the user record.
    /// </summary>
    public static async Task EnsureSystemLeaguesAsync(
        AppDbContext db,
        IUserContext user,
        string? countryCode,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return;
        }

        var normalizedCountry = NormalizeCountryCode(countryCode);
        await PersistCountryCodeAsync(db, user, normalizedCountry, ct);

        var displayName = await ResolveDefaultDisplayNameAsync(db, user, ct);

        await EnsureGlobalLeagueAsync(db, user, displayName, ct);
        await EnsureCountryLeagueAsync(db, user, normalizedCountry, displayName, ct);
    }

    public static async Task<(int CustomUsed, int TotalUsed)> CountMembershipsAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var query = db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .Include(m => m.League);

        var total = await query.CountAsync(ct);
        var custom = await query.CountAsync(m => m.League.Kind == LeagueKind.Custom, ct);
        return (custom, total);
    }

    public static LeagueLimitsResponse BuildLimits(int customUsed, int totalUsed) =>
        new(customUsed, League.MaxCustomLeaguesPerUser, totalUsed, League.MaxTotalLeagueMemberships);

    private static async Task PersistCountryCodeAsync(
        AppDbContext db,
        IUserContext user,
        string countryCode,
        CancellationToken ct)
    {
        if (user.IsAuthenticated && user.UserId.HasValue)
        {
            var registered = await db.Users.FindAsync([user.UserId.Value], ct);
            if (registered is not null)
            {
                registered.CountryCode = countryCode;
            }
        }
        else if (user.AnonymousUserId.HasValue)
        {
            var anon = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], ct);
            if (anon is not null)
            {
                anon.CountryCode = countryCode;
            }
        }
    }

    private static async Task<string> ResolveDefaultDisplayNameAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        var existing = await db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .OrderByDescending(m => m.JoinedAt)
            .Select(m => m.DisplayName)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        if (user.IsAuthenticated && user.UserId.HasValue)
        {
            var registered = await db.Users.FindAsync([user.UserId.Value], ct);
            if (!string.IsNullOrWhiteSpace(registered?.DisplayName))
            {
                return registered.DisplayName;
            }
        }

        if (user.AnonymousUserId.HasValue)
        {
            return $"Guest-{user.AnonymousUserId.Value.ToString()[..4].ToUpperInvariant()}";
        }

        return "Player";
    }

    private static async Task EnsureGlobalLeagueAsync(
        AppDbContext db,
        IUserContext user,
        string displayName,
        CancellationToken ct)
    {
        var league = await db.Leagues.FindAsync([League.GlobalLeagueId], ct);
        if (league is null)
        {
            league = new League
            {
                Id = League.GlobalLeagueId,
                Name = "Global Banter League",
                InviteCode = "GLOBAL",
                Kind = LeagueKind.Global,
                MaxMembers = 1_000_000
            };
            db.Leagues.Add(league);
        }

        await EnsureMembershipAsync(db, league, user, displayName, ct);
    }

    private static async Task EnsureCountryLeagueAsync(
        AppDbContext db,
        IUserContext user,
        string countryCode,
        string displayName,
        CancellationToken ct)
    {
        var league = await db.Leagues
            .FirstOrDefaultAsync(l => l.Kind == LeagueKind.Country && l.CountryCode == countryCode, ct);

        if (league is null)
        {
            league = new League
            {
                Id = Guid.NewGuid(),
                Name = CountryLeagueName(countryCode),
                InviteCode = $"CTRY{countryCode}",
                Kind = LeagueKind.Country,
                CountryCode = countryCode,
                MaxMembers = 500_000
            };
            db.Leagues.Add(league);
        }

        await EnsureMembershipAsync(db, league, user, displayName, ct);
    }

    private static async Task EnsureMembershipAsync(
        AppDbContext db,
        League league,
        IUserContext user,
        string displayName,
        CancellationToken ct)
    {
        var exists = await db.LeagueMembers.AnyAsync(m =>
            m.LeagueId == league.Id &&
            (user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId), ct);

        if (exists)
        {
            return;
        }

        db.LeagueMembers.Add(new LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            UserId = user.UserId,
            AnonymousUserId = user.IsAuthenticated ? null : user.AnonymousUserId,
            DisplayName = displayName,
            IsAdmin = false
        });
    }
}
