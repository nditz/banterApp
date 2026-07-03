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

    /// <summary>Returns a normalized 2-letter code, or null when none/invalid (no GB default).</summary>
    public static string? NormalizeCountryCodeOrNull(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 2)
        {
            return null;
        }

        return code.Trim().ToUpperInvariant();
    }

    public static string CountryLeagueName(string countryCode) =>
        CountryNames.TryGetValue(countryCode, out var name)
            ? $"{name} Fans"
            : $"{countryCode} Fans";

    /// <summary>
    /// Ensures global + country system league rows exist (no membership).
    /// Called for every leagues list so guests always see system leagues.
    /// </summary>
    public static async Task EnsureSystemLeagueRowsAsync(
        AppDbContext db,
        string? countryCode,
        CancellationToken ct)
    {
        var normalizedCountry = NormalizeCountryCode(countryCode);
        await GetOrCreateGlobalLeagueAsync(db, ct);
        await GetOrCreateCountryLeagueAsync(db, normalizedCountry, ct);
    }

    /// <summary>
    /// Ensures the user is enrolled in the Global league, and — only when an explicit,
    /// valid <paramref name="countryCode"/> is provided — also creates/joins that Country
    /// league and persists the choice. A null/invalid code means Global only.
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

        var displayName = await ResolveDefaultDisplayNameAsync(db, user, ct);

        var global = await GetOrCreateGlobalLeagueAsync(db, ct);
        await EnsureMembershipAsync(db, global, user, displayName, ct);

        var normalizedCountry = NormalizeCountryCodeOrNull(countryCode);
        await RemoveOtherCountryLeagueMembershipsAsync(db, user, normalizedCountry, ct);

        if (normalizedCountry is null)
        {
            await PersistCountryCodeAsync(db, user, null, ct);
            return;
        }

        await PersistCountryCodeAsync(db, user, normalizedCountry, ct);
        var country = await GetOrCreateCountryLeagueAsync(db, normalizedCountry, ct);
        await EnsureMembershipAsync(db, country, user, displayName, ct);
    }

    /// <summary>
    /// Enrollment for auto/refresh call sites (login sync, leagues list): joins Global and,
    /// only if the user already chose a country previously, keeps their Country league.
    /// Never derives a country league from a browser-locale header.
    /// </summary>
    public static async Task EnsureSystemLeaguesForSessionAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return;
        }

        var persisted = await GetPersistedCountryCodeAsync(db, user, ct);
        await EnsureSystemLeaguesAsync(db, user, persisted, ct);
    }

    public static async Task<string?> GetPersistedCountryCodeAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (user.IsAuthenticated && user.UserId.HasValue)
        {
            var registered = await db.Users.FindAsync([user.UserId.Value], ct);
            return registered?.CountryCode;
        }

        if (user.AnonymousUserId.HasValue)
        {
            var anon = await db.AnonymousUsers.FindAsync([user.AnonymousUserId.Value], ct);
            return anon?.CountryCode;
        }

        return null;
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
        string? countryCode,
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

    /// <summary>
    /// Drops country-system-league memberships that no longer match the user's chosen country.
    /// </summary>
    private static async Task RemoveOtherCountryLeagueMembershipsAsync(
        AppDbContext db,
        IUserContext user,
        string? keepCountryCode,
        CancellationToken ct)
    {
        var stale = await (
            from member in db.LeagueMembers
            join league in db.Leagues on member.LeagueId equals league.Id
            where league.Kind == LeagueKind.Country
                  && (user.IsAuthenticated
                      ? member.UserId == user.UserId
                      : member.AnonymousUserId == user.AnonymousUserId)
                  && (keepCountryCode == null
                      || league.CountryCode != keepCountryCode)
            select member).ToListAsync(ct);

        if (stale.Count == 0)
        {
            return;
        }

        db.LeagueMembers.RemoveRange(stale);
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

        return await LeagueDisplayNameResolver.ResolveAsync(db, user, ct);
    }

    private static async Task<League> GetOrCreateGlobalLeagueAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var league = await db.Leagues.FindAsync([League.GlobalLeagueId], ct);
        if (league is not null)
        {
            return league;
        }

        league = new League
        {
            Id = League.GlobalLeagueId,
            Name = "Global Banter League",
            InviteCode = "GLOBAL",
            Kind = LeagueKind.Global,
            MaxMembers = 1_000_000
        };
        db.Leagues.Add(league);
        return league;
    }

    private static async Task<League> GetOrCreateCountryLeagueAsync(
        AppDbContext db,
        string countryCode,
        CancellationToken ct)
    {
        var league = await db.Leagues
            .FirstOrDefaultAsync(l => l.Kind == LeagueKind.Country && l.CountryCode == countryCode, ct);

        if (league is not null)
        {
            return league;
        }

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
        return league;
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
