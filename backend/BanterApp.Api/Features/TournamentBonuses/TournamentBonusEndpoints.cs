using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.TournamentBonuses;

public static class TournamentBonusEndpoints
{
    public static IEndpointRouteBuilder MapTournamentBonusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tournament-bonuses").WithTags("Tournament Bonuses");

        group.MapGet("/", GetTournamentBonuses);
        group.MapGet("/players", SearchPlayers);
        group.MapPut("/pick", SaveTournamentBonusPick)
            .RequireRateLimiting("write")
            .WithValidation<SaveTournamentBonusPickRequest>();

        return app;
    }

    private static async Task<IResult> SearchPlayers(
        AppDbContext db,
        IUserContext user,
        PlayerDirectory directory,
        HttpContext http,
        string? query,
        string? teamCode,
        int? limit,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        var take = Math.Clamp(limit ?? 25, 1, 50);
        var normalizedTeam = string.IsNullOrWhiteSpace(teamCode)
            ? null
            : teamCode.Trim().ToUpperInvariant();
        var trimmedQuery = query?.Trim() ?? string.Empty;

        // Live lineup names (real synced squads) merged with the curated directory so that
        // both known stars and freshly-synced players are searchable.
        var lineupQuery = db.LineupPlayers.AsNoTracking().AsQueryable();
        if (normalizedTeam is not null)
        {
            lineupQuery = lineupQuery.Where(p => p.TeamCode == normalizedTeam);
        }

        if (trimmedQuery.Length > 0)
        {
            var loweredQuery = trimmedQuery.ToLower();
            lineupQuery = lineupQuery.Where(p => p.PlayerName.ToLower().Contains(loweredQuery));
        }

        var lineupPlayers = await lineupQuery
            .Select(p => new { p.PlayerName, p.TeamCode })
            .Distinct()
            .Take(200)
            .ToListAsync(ct);

        // Map team codes to display names (directory first, fall back to match team names).
        var matchTeamNames = await db.Matches
            .AsNoTracking()
            .Where(m => m.TeamACode != "" || m.TeamBCode != "")
            .SelectMany(m => new[]
            {
                new { Code = m.TeamACode, Name = m.TeamA },
                new { Code = m.TeamBCode, Name = m.TeamB }
            })
            .Where(t => t.Code != "" && t.Code != "TBD")
            .Distinct()
            .ToListAsync(ct);

        var teamNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in matchTeamNames)
        {
            teamNameMap[t.Code] = t.Name;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<TournamentBonusPlayerOption>();

        foreach (var player in directory.Search(trimmedQuery, normalizedTeam, take))
        {
            var teamName = teamNameMap.GetValueOrDefault(player.TeamCode) ?? player.TeamName;
            var key = $"{TournamentBonusScoringService.NormalizePlayerName(player.PlayerName)}|{player.TeamCode}";
            if (seen.Add(key))
            {
                results.Add(new TournamentBonusPlayerOption(player.PlayerName, player.TeamCode, teamName));
            }
        }

        foreach (var player in lineupPlayers)
        {
            var key = $"{TournamentBonusScoringService.NormalizePlayerName(player.PlayerName)}|{player.TeamCode}";
            if (seen.Add(key))
            {
                var teamName = teamNameMap.GetValueOrDefault(player.TeamCode)
                    ?? directory.GetTeamName(player.TeamCode)
                    ?? player.TeamCode;
                results.Add(new TournamentBonusPlayerOption(player.PlayerName, player.TeamCode, teamName));
            }
        }

        // Directory (relevance-ranked) already leads; live-only players follow. Cap the merged set.
        var ordered = results.Take(take).ToList();

        return Results.Ok(new TournamentBonusPlayerSearchResponse(ordered));
    }

    private static async Task<IResult> GetTournamentBonuses(
        AppDbContext db,
        IUserContext user,
        TournamentBonusScoringService scoring,
        HttpContext http,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        await scoring.LockPicksIfNeededAsync(db, ct);
        await scoring.RescoreAllPicksAsync(db, ct);

        var eligibility = await scoring.CheckEligibilityAsync(db, user, ct);
        var isLocked = await scoring.IsLockedAsync(db, ct);

        var picks = await db.TournamentBonusPicks
            .Where(p => user.IsAuthenticated
                ? p.UserId == user.UserId
                : p.AnonymousUserId == user.AnonymousUserId)
            .ToListAsync(ct);

        var awards = await db.TournamentAwardResults.ToListAsync(ct);
        var awardMap = awards.ToDictionary(a => a.Category);

        var teams = await db.Matches
            .AsNoTracking()
            .SelectMany(m => new[]
            {
                new { Code = m.TeamACode, Name = m.TeamA },
                new { Code = m.TeamBCode, Name = m.TeamB }
            })
            .Where(t => t.Code != string.Empty)
            .Distinct()
            .OrderBy(t => t.Name)
            .Select(t => new TournamentBonusTeamOption(t.Code, t.Name))
            .ToListAsync(ct);

        var players = await db.LineupPlayers
            .AsNoTracking()
            .Select(p => p.PlayerName)
            .Distinct()
            .OrderBy(name => name)
            .Take(500)
            .ToListAsync(ct);

        var categories = Enum.GetValues<TournamentBonusCategory>()
            .Select(category =>
            {
                var pick = picks.FirstOrDefault(p => p.Category == category);
                awardMap.TryGetValue(category, out var award);

                return new TournamentBonusCategoryInfo(
                    TournamentBonusCategoryJsonConverter.ToApiString(category),
                    TournamentBonusScoringService.CategoryLabel(category),
                    TournamentBonusScoringService.CategoryDescription(category),
                    TournamentBonusScoringService.PointsForCategory(category),
                    TournamentBonusScoringService.IsTeamCategory(category),
                    pick is null
                        ? null
                        : new TournamentBonusPickResponse(
                            pick.Id,
                            TournamentBonusCategoryJsonConverter.ToApiString(pick.Category),
                            pick.PickValue,
                            pick.PointsAwarded,
                            pick.LockedAt,
                            pick.CreatedAt),
                    award is null
                        ? null
                        : new TournamentBonusAwardResponse(
                            TournamentBonusCategoryJsonConverter.ToApiString(award.Category),
                            award.AnswerValue,
                            award.AnswerDisplay,
                            award.AnnouncedAt));
            })
            .ToList();

        return Results.Ok(new TournamentBonusStatusResponse(
            eligibility.IsEligible,
            eligibility.HasActivity,
            eligibility.HasQualifyingLeague,
            eligibility.Reasons,
            isLocked,
            !isLocked,
            categories,
            teams,
            players));
    }

    private static async Task<IResult> SaveTournamentBonusPick(
        SaveTournamentBonusPickRequest request,
        AppDbContext db,
        IUserContext user,
        TournamentBonusScoringService scoring,
        HttpContext http,
        TurnstileService turnstile,
        CancellationToken ct)
    {
        var guard = await SessionGuard.RequireActiveSessionAsync(user, http, db, ct);
        if (guard is not null)
        {
            return guard;
        }

        if (await scoring.IsLockedAsync(db, ct))
        {
            return Results.BadRequest(new { error = "Tournament bonus picks locked at kickoff." });
        }

        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        var pickValue = request.PickValue.Trim();
        if (TournamentBonusScoringService.IsTeamCategory(request.Category))
        {
            pickValue = TournamentBonusScoringService.NormalizeTeamCode(pickValue);
            var teamExists = await db.Matches.AnyAsync(m =>
                m.TeamACode == pickValue || m.TeamBCode == pickValue, ct);
            if (!teamExists)
            {
                return Results.BadRequest(new { error = "Pick a team from the tournament." });
            }
        }
        else
        {
            pickValue = TournamentBonusScoringService.NormalizePlayerName(pickValue);
        }

        var awards = await db.TournamentAwardResults.ToListAsync(ct);
        var awardMap = awards.ToDictionary(a => a.Category);
        var points = scoring.CalculatePoints(request.Category, pickValue, awardMap);

        var existing = await db.TournamentBonusPicks.FirstOrDefaultAsync(p =>
            p.Category == request.Category &&
            (user.IsAuthenticated
                ? p.UserId == user.UserId
                : p.AnonymousUserId == user.AnonymousUserId), ct);

        if (existing is null)
        {
            existing = new TournamentBonusPick
            {
                Id = Guid.NewGuid(),
                UserId = user.IsAuthenticated ? user.UserId : null,
                AnonymousUserId = user.IsAnonymous ? user.AnonymousUserId : null,
                Category = request.Category,
                PickValue = pickValue,
                PointsAwarded = points
            };
            db.TournamentBonusPicks.Add(existing);
        }
        else
        {
            if (existing.LockedAt is not null)
            {
                return Results.BadRequest(new { error = "This bonus pick is locked." });
            }

            existing.PickValue = pickValue;
            existing.PointsAwarded = points;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new TournamentBonusPickResponse(
            existing.Id,
            TournamentBonusCategoryJsonConverter.ToApiString(existing.Category),
            existing.PickValue,
            existing.PointsAwarded,
            existing.LockedAt,
            existing.CreatedAt));
    }
}

public sealed record TournamentBonusPickResponse(
    Guid Id,
    string Category,
    string PickValue,
    int PointsAwarded,
    DateTimeOffset? LockedAt,
    DateTimeOffset CreatedAt);

public sealed record TournamentBonusAwardResponse(
    string Category,
    string AnswerValue,
    string? AnswerDisplay,
    DateTimeOffset AnnouncedAt);

public sealed record TournamentBonusCategoryInfo(
    string Category,
    string Label,
    string Description,
    int Points,
    bool IsTeamPick,
    TournamentBonusPickResponse? Pick,
    TournamentBonusAwardResponse? OfficialResult);

public sealed record TournamentBonusTeamOption(string Code, string Name);

public sealed record TournamentBonusPlayerOption(string Name, string TeamCode, string TeamName);

public sealed record TournamentBonusPlayerSearchResponse(
    IReadOnlyList<TournamentBonusPlayerOption> Players);

public sealed record TournamentBonusStatusResponse(
    bool IsEligible,
    bool HasActivity,
    bool HasQualifyingLeague,
    IReadOnlyList<string> IneligibilityReasons,
    bool IsLocked,
    bool CanPick,
    IReadOnlyList<TournamentBonusCategoryInfo> Categories,
    IReadOnlyList<TournamentBonusTeamOption> Teams,
    IReadOnlyList<string> PlayerSuggestions);

public record SaveTournamentBonusPickRequest(
    TournamentBonusCategory Category,
    string PickValue,
    string? TurnstileToken);

public sealed class SaveTournamentBonusPickValidator : AbstractValidator<SaveTournamentBonusPickRequest>
{
    public SaveTournamentBonusPickValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.PickValue).NotEmpty().MaximumLength(100);
    }
}
