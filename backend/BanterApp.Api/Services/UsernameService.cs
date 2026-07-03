using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Integrations.Ai;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public sealed class UsernameService(AppDbContext db, IContentGenerator contentGenerator)
{
    private static readonly string[] FallbackUsernames =
    [
        "Shadowfox", "Ironhelm", "MysticWolf", "Thunderblade", "NightOwl",
        "StormRider", "SilverMage", "DragonHeart", "StarSeeker", "FrostGiant",
        "EmberKnight", "RuneWalker", "CrystalBow", "MoonStriker", "GoldenShield",
        "WildPhoenix", "StoneGuard", "SwiftArrow", "BraveTitan", "LuckyRogue"
    ];

    public async Task<string> SuggestUsernameAsync(CancellationToken cancellationToken = default)
    {
        string? candidate = null;
        try
        {
            candidate = UsernameRules.Sanitize(
                await contentGenerator.GenerateUsernameSuggestionAsync(cancellationToken));
        }
        catch
        {
            // AI unavailable — fall through to deterministic suggestions.
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = FallbackUsernames[Random.Shared.Next(FallbackUsernames.Length)];
        }

        return await EnsureAvailableAsync(candidate, cancellationToken);
    }

    public async Task<(bool Success, string? Error, string? Username)> ApplyUsernameAsync(
        Guid anonymousUserId,
        string rawUsername,
        CancellationToken cancellationToken = default)
    {
        var username = UsernameRules.Sanitize(rawUsername);
        if (username is null || !UsernameRules.IsValidFormat(username))
        {
            return (false, "Username must be 3–20 characters and use only letters A–Z and numbers 0–9.", null);
        }

        if (await IsTakenAsync(username, anonymousUserId, cancellationToken))
        {
            return (false, "That username is already taken. Try another one.", null);
        }

        var anonymous = await db.AnonymousUsers.FindAsync([anonymousUserId], cancellationToken);
        if (anonymous is null)
        {
            return (false, "Session not found.", null);
        }

        anonymous.Username = username;
        await db.SaveChangesAsync(cancellationToken);
        await SyncLeagueDisplayNamesAsync(anonymousUserId, username, cancellationToken);

        return (true, null, username);
    }

    public async Task<bool> IsTakenAsync(
        string username,
        Guid? excludeAnonymousUserId = null,
        CancellationToken cancellationToken = default)
    {
        var key = UsernameRules.NormalizeKey(username);
        var query = db.AnonymousUsers.AsNoTracking()
            .Where(a => a.Username != null && a.Username.ToLower() == key);

        if (excludeAnonymousUserId is not null)
        {
            query = query.Where(a => a.Id != excludeAnonymousUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task<string> EnsureAvailableAsync(string baseName, CancellationToken cancellationToken)
    {
        var candidate = baseName;
        if (!await IsTakenAsync(candidate, cancellationToken: cancellationToken))
        {
            return candidate;
        }

        for (var i = 2; i < 100; i++)
        {
            var suffix = i.ToString();
            var maxBase = UsernameRules.MaxLength - suffix.Length;
            var trimmed = baseName.Length > maxBase ? baseName[..maxBase] : baseName;
            candidate = $"{trimmed}{suffix}";
            if (!await IsTakenAsync(candidate, cancellationToken: cancellationToken))
            {
                return candidate;
            }
        }

        return $"{baseName[..Math.Min(12, baseName.Length)]}{Random.Shared.Next(100, 999)}";
    }

    private async Task SyncLeagueDisplayNamesAsync(
        Guid anonymousUserId,
        string username,
        CancellationToken cancellationToken)
    {
        var memberships = await db.LeagueMembers
            .Where(m => m.AnonymousUserId == anonymousUserId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return;
        }

        foreach (var group in memberships.GroupBy(m => m.LeagueId))
        {
            var taken = await db.LeagueMembers.AsNoTracking()
                .Where(m => m.LeagueId == group.Key && m.AnonymousUserId != anonymousUserId)
                .Select(m => m.DisplayName)
                .ToListAsync(cancellationToken);

            var displayName = LeagueDisplayNameResolver.EnsureUniqueInLeague(username, taken);
            foreach (var member in group)
            {
                member.DisplayName = displayName;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
