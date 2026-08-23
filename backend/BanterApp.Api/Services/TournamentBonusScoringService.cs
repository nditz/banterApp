using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public sealed class TournamentBonusScoringService(IConfiguration configuration)
{
    public const int MinCustomLeagueMembers = 3;

    public const int PlayerOfTheSeasonPoints = 50;
    public const int GoldenBootPoints = 40;
    public const int MostAssistsPoints = 35;
    public const int GoldenGlovePoints = 35;
    public const int SurpriseTeamPoints = 30;
    public const int LeagueWinnerPoints = 50;
    public const int TopFourClubPoints = 20;
    public const int RelegatedClubPoints = 20;
    public const int YoungPlayerPoints = 35;

    public static int SlotCount(TournamentBonusCategory category) =>
        category switch
        {
            TournamentBonusCategory.TopFour => 4,
            TournamentBonusCategory.Relegated => 3,
            _ => 1
        };

    public static int PointsForCategory(TournamentBonusCategory category) =>
        category switch
        {
            TournamentBonusCategory.PlayerOfTheSeason => PlayerOfTheSeasonPoints,
            TournamentBonusCategory.GoldenBoot => GoldenBootPoints,
            TournamentBonusCategory.MostAssists => MostAssistsPoints,
            TournamentBonusCategory.GoldenGlove => GoldenGlovePoints,
            TournamentBonusCategory.SurpriseTeam => SurpriseTeamPoints,
            TournamentBonusCategory.LeagueWinner => LeagueWinnerPoints,
            TournamentBonusCategory.TopFour => TopFourClubPoints,
            TournamentBonusCategory.Relegated => RelegatedClubPoints,
            TournamentBonusCategory.YoungPlayerOfTheSeason => YoungPlayerPoints,
            _ => 0
        };

    public static string CategoryLabel(TournamentBonusCategory category) =>
        category switch
        {
            TournamentBonusCategory.PlayerOfTheSeason => "Player of the Season",
            TournamentBonusCategory.GoldenBoot => "Golden Boot",
            TournamentBonusCategory.MostAssists => "Most assists",
            TournamentBonusCategory.GoldenGlove => "Golden Glove",
            TournamentBonusCategory.SurpriseTeam => "Surprise team",
            TournamentBonusCategory.LeagueWinner => "League winner",
            TournamentBonusCategory.TopFour => "Top four",
            TournamentBonusCategory.Relegated => "Relegated",
            TournamentBonusCategory.YoungPlayerOfTheSeason => "Young Player of the Season",
            _ => category.ToString()
        };

    public static string CategoryDescription(TournamentBonusCategory category) =>
        category switch
        {
            TournamentBonusCategory.PlayerOfTheSeason =>
                "Who wins the Premier League Player of the Season award?",
            TournamentBonusCategory.GoldenBoot =>
                "Who finishes as the Premier League's leading goal scorer?",
            TournamentBonusCategory.MostAssists =>
                "Who leads the Premier League in assists?",
            TournamentBonusCategory.GoldenGlove =>
                "Which goalkeeper wins the Golden Glove?",
            TournamentBonusCategory.SurpriseTeam =>
                "Which club exceeds expectations this season?",
            TournamentBonusCategory.LeagueWinner =>
                "Who lifts the Premier League trophy?",
            TournamentBonusCategory.TopFour =>
                "Name the four clubs that finish in Champions League places.",
            TournamentBonusCategory.Relegated =>
                "Name the three clubs that go down.",
            TournamentBonusCategory.YoungPlayerOfTheSeason =>
                "Who wins Young Player of the Season?",
            _ => string.Empty
        };

    public static bool IsTeamCategory(TournamentBonusCategory category) =>
        category is TournamentBonusCategory.SurpriseTeam
            or TournamentBonusCategory.LeagueWinner
            or TournamentBonusCategory.TopFour
            or TournamentBonusCategory.Relegated;

    public int CalculatePoints(
        TournamentBonusCategory category,
        string pickValue,
        IReadOnlyDictionary<TournamentBonusCategory, TournamentAwardResult> awards)
    {
        if (!awards.TryGetValue(category, out var award))
        {
            return 0;
        }

        return IsCorrectPick(category, pickValue, award.AnswerValue)
            ? PointsForCategory(category)
            : 0;
    }

    public static bool IsCorrectPick(
        TournamentBonusCategory category,
        string pickValue,
        string answerValue)
    {
        if (IsTeamCategory(category))
        {
            var answers = answerValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeTeamCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return answers.Contains(NormalizeTeamCode(pickValue));
        }

        return string.Equals(
            NormalizePlayerName(pickValue),
            NormalizePlayerName(answerValue),
            StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizePlayerName(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    public static string NormalizeTeamCode(string value) =>
        value.Trim().ToUpperInvariant();

    public async Task<bool> IsLockedAsync(AppDbContext db, CancellationToken ct)
    {
        if (configuration.GetValue("SeasonAwards:DisablePickLock", false) ||
            configuration.GetValue("Tournament:DisableBonusPickLock", false))
        {
            return false;
        }

        if (DateTimeOffset.TryParse(
                configuration["SeasonAwards:PicksLockUtc"] ?? configuration["Tournament:BonusPicksLockUtc"],
                out var configuredLock))
        {
            return DateTimeOffset.UtcNow >= configuredLock.ToUniversalTime();
        }

        var firstKickoff = await db.Matches
            .OrderBy(m => m.KickoffTime)
            .Select(m => (DateTimeOffset?)m.KickoffTime)
            .FirstOrDefaultAsync(ct);

        return firstKickoff.HasValue && DateTimeOffset.UtcNow >= firstKickoff.Value;
    }

    public async Task<TournamentBonusEligibilityResult> CheckEligibilityAsync(
        AppDbContext db,
        IUserContext user,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated && !user.IsAnonymous)
        {
            return new TournamentBonusEligibilityResult(false, false, false, ["Accept terms to unlock season awards."]);
        }

        var hasActivity = await db.Predictions.AnyAsync(p =>
            user.IsAuthenticated
                ? p.UserId == user.UserId
                : p.AnonymousUserId == user.AnonymousUserId, ct);

        var customLeagueIds = await db.LeagueMembers
            .Where(m => user.IsAuthenticated
                ? m.UserId == user.UserId
                : m.AnonymousUserId == user.AnonymousUserId)
            .Where(m => m.League.Kind == LeagueKind.Custom)
            .Select(m => m.LeagueId)
            .Distinct()
            .ToListAsync(ct);

        var hasQualifyingLeague = false;
        if (customLeagueIds.Count > 0)
        {
            var memberCounts = await db.LeagueMembers
                .Where(m => customLeagueIds.Contains(m.LeagueId))
                .GroupBy(m => m.LeagueId)
                .Select(g => new { LeagueId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            hasQualifyingLeague = memberCounts.Any(x => x.Count >= MinCustomLeagueMembers);
        }

        var reasons = new List<string>();
        if (!hasActivity)
        {
            reasons.Add("Make at least one match prediction for award points to count on qualifying league leaderboards.");
        }

        if (!hasQualifyingLeague)
        {
            reasons.Add(
                $"Join a private league with at least {MinCustomLeagueMembers} members for award points to count on that league's leaderboard.");
        }

        return new TournamentBonusEligibilityResult(
            hasActivity && hasQualifyingLeague,
            hasActivity,
            hasQualifyingLeague,
            reasons);
    }

    public async Task RescoreAllPicksAsync(AppDbContext db, CancellationToken ct)
    {
        var awards = await db.TournamentAwardResults.ToListAsync(ct);
        if (awards.Count == 0)
        {
            return;
        }

        var awardMap = awards.ToDictionary(a => a.Category);
        var picks = await db.TournamentBonusPicks.ToListAsync(ct);
        var changed = false;

        foreach (var pick in picks)
        {
            var newPoints = CalculatePoints(pick.Category, pick.PickValue, awardMap);
            if (pick.PointsAwarded != newPoints)
            {
                pick.PointsAwarded = newPoints;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task LockPicksIfNeededAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await IsLockedAsync(db, ct))
        {
            return;
        }

        var unlocked = await db.TournamentBonusPicks
            .Where(p => p.LockedAt == null)
            .ToListAsync(ct);

        if (unlocked.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var pick in unlocked)
        {
            pick.LockedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public static bool BonusPointsApplyToLeague(League league, int memberCount) =>
        league.Kind == LeagueKind.Custom && memberCount >= MinCustomLeagueMembers;

    public async Task<int> GetBonusPointsAsync(AppDbContext db, IUserContext user, CancellationToken ct)
    {
        var picks = await db.TournamentBonusPicks
            .Where(p => user.IsAuthenticated
                ? p.UserId == user.UserId
                : p.AnonymousUserId == user.AnonymousUserId)
            .ToListAsync(ct);

        return picks.Sum(p => p.PointsAwarded);
    }

    public async Task<int> GetMatchPointsAsync(AppDbContext db, IUserContext user, CancellationToken ct)
    {
        var query = user.IsAuthenticated
            ? db.Predictions.Where(p => p.UserId == user.UserId)
            : db.Predictions.Where(p => p.AnonymousUserId == user.AnonymousUserId);

        var matchPoints = await query.SumAsync(p => p.PointsAwarded, ct);
        var weekBonus = user.IsAuthenticated
            ? await db.MatchweekBonuses.Where(b => b.UserId == user.UserId).SumAsync(b => b.PointsAwarded, ct)
            : await db.MatchweekBonuses.Where(b => b.AnonymousUserId == user.AnonymousUserId).SumAsync(b => b.PointsAwarded, ct);
        return matchPoints + weekBonus;
    }

    public async Task<Dictionary<Guid, int>> GetBonusPointsByIdentityAsync(
        AppDbContext db,
        IReadOnlyList<LeagueMember> members,
        CancellationToken ct)
    {
        var userIds = members.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).ToList();
        var anonIds = members.Where(m => m.AnonymousUserId.HasValue).Select(m => m.AnonymousUserId!.Value).ToList();

        var userBonus = await db.TournamentBonusPicks
            .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
            .GroupBy(p => p.UserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded) })
            .ToListAsync(ct);

        var anonBonus = await db.TournamentBonusPicks
            .Where(p => p.AnonymousUserId.HasValue && anonIds.Contains(p.AnonymousUserId.Value))
            .GroupBy(p => p.AnonymousUserId!.Value)
            .Select(g => new { Id = g.Key, Points = g.Sum(p => p.PointsAwarded) })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, int>();
        foreach (var entry in userBonus.Concat(anonBonus))
        {
            map[entry.Id] = entry.Points;
        }

        return map;
    }
}

public sealed record TournamentBonusEligibilityResult(
    bool IsEligible,
    bool HasActivity,
    bool HasQualifyingLeague,
    IReadOnlyList<string> Reasons);
