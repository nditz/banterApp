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

        var syncedQuery = db.Players.AsNoTracking().Where(p => p.IsActive && p.ClubName != null);
        if (normalizedTeam is not null)
        {
            syncedQuery = syncedQuery.Where(p =>
                p.ClubName != null && p.ClubName.ToLower().Contains(normalizedTeam.ToLower()));
        }

        if (trimmedQuery.Length > 0)
        {
            var loweredQuery = trimmedQuery.ToLower();
            syncedQuery = syncedQuery.Where(p =>
                p.DisplayName.ToLower().Contains(loweredQuery) ||
                (p.KnownName != null && p.KnownName.ToLower().Contains(loweredQuery)));
        }

        var syncedPlayers = await syncedQuery
            .Include(p => p.Country)
            .OrderBy(p => p.DisplayName)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.DisplayName,
                TeamCode = p.Country != null ? p.Country.Code ?? "" : "",
                TeamName = p.Country != null ? p.Country.Name : p.NationalTeamName ?? "",
                p.PhotoUrl,
                p.ClubName
            })
            .ToListAsync(ct);

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

        var teamNameMap = await LoadTeamNameMapAsync(db, ct);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<TournamentBonusPlayerOption>();

        foreach (var player in syncedPlayers)
        {
            var clubName = player.ClubName ?? player.TeamName ?? "Premier League";
            var syncedTeamCode = string.IsNullOrWhiteSpace(player.TeamCode) ? "PL" : player.TeamCode;
            var key = $"{TournamentBonusScoringService.NormalizePlayerName(player.DisplayName)}|{clubName}";
            if (seen.Add(key))
            {
                results.Add(new TournamentBonusPlayerOption(
                    player.DisplayName, syncedTeamCode, clubName, player.Id, player.PhotoUrl, player.ClubName));
            }
        }

        foreach (var player in lineupPlayers)
        {
            var lineupTeamCode = player.TeamCode;
            var key = $"{TournamentBonusScoringService.NormalizePlayerName(player.PlayerName)}|{lineupTeamCode}";
            if (seen.Add(key))
            {
                var teamName = teamNameMap.GetValueOrDefault(lineupTeamCode)
                    ?? lineupTeamCode;
                results.Add(new TournamentBonusPlayerOption(player.PlayerName, lineupTeamCode, teamName));
            }
        }

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

        var teams = await LoadTeamsAsync(db, ct);

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
                var categoryPicks = picks.Where(p => p.Category == category).OrderBy(p => p.SlotIndex).ToList();
                var pick = categoryPicks.FirstOrDefault();
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
                            pick.CreatedAt,
                            pick.SlotIndex),
                    award is null
                        ? null
                        : new TournamentBonusAwardResponse(
                            TournamentBonusCategoryJsonConverter.ToApiString(award.Category),
                            award.AnswerValue,
                            award.AnswerDisplay,
                            award.AnnouncedAt),
                    TournamentBonusScoringService.SlotCount(category),
                    categoryPicks.Select(p => new TournamentBonusPickResponse(
                        p.Id,
                        TournamentBonusCategoryJsonConverter.ToApiString(p.Category),
                        p.PickValue,
                        p.PointsAwarded,
                        p.LockedAt,
                        p.CreatedAt,
                        p.SlotIndex)).ToList());
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
            return Results.BadRequest(new { error = "Season awards locked at the first Premier League kickoff." });
        }

        if (request.SlotIndex < 0 || request.SlotIndex >= TournamentBonusScoringService.SlotCount(request.Category))
        {
            return Results.BadRequest(new { error = "Invalid award slot." });
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
                return Results.BadRequest(new { error = "Pick a Premier League club." });
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
            p.SlotIndex == request.SlotIndex &&
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
                SlotIndex = request.SlotIndex,
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
            existing.CreatedAt,
            existing.SlotIndex));
    }

    private static async Task<Dictionary<string, string>> LoadTeamNameMapAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var rows = await db.Matches
            .AsNoTracking()
            .Select(m => new { m.TeamACode, m.TeamA, m.TeamBCode, m.TeamB })
            .ToListAsync(ct);

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.TeamACode) && row.TeamACode != "TBD")
            {
                map[row.TeamACode] = row.TeamA;
            }

            if (!string.IsNullOrEmpty(row.TeamBCode) && row.TeamBCode != "TBD")
            {
                map[row.TeamBCode] = row.TeamB;
            }
        }

        return map;
    }

    private static List<TournamentBonusTeamOption> DedupeTeamsByCode(
        IReadOnlyList<TournamentBonusTeamOption> teams)
    {
        var byCode = new Dictionary<string, TournamentBonusTeamOption>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in teams)
        {
            if (!byCode.ContainsKey(team.Code))
            {
                byCode[team.Code] = team;
            }
        }

        return byCode.Values
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<TournamentBonusTeamOption>> LoadTeamsAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var synced = await db.ClubTeams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TournamentBonusTeamOption(t.Code, t.Name))
            .ToListAsync(ct);

        if (synced.Count > 0)
        {
            return DedupeTeamsByCode(synced);
        }

        var map = await LoadTeamNameMapAsync(db, ct);
        return DedupeTeamsByCode(
            map
                .Select(kv => new TournamentBonusTeamOption(kv.Key, kv.Value))
                .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }
}

public sealed record TournamentBonusPickResponse(
    Guid Id,
    string Category,
    string PickValue,
    int PointsAwarded,
    DateTimeOffset? LockedAt,
    DateTimeOffset CreatedAt,
    int SlotIndex = 0);

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
    TournamentBonusAwardResponse? OfficialResult,
    int SlotCount = 1,
    IReadOnlyList<TournamentBonusPickResponse>? Picks = null);

public sealed record TournamentBonusTeamOption(string Code, string Name);

public sealed record TournamentBonusPlayerOption(
    string Name,
    string TeamCode,
    string TeamName,
    Guid? PlayerId = null,
    string? PhotoUrl = null,
    string? ClubName = null);

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
    string? TurnstileToken,
    int SlotIndex = 0);

public sealed class SaveTournamentBonusPickValidator : AbstractValidator<SaveTournamentBonusPickRequest>
{
    public SaveTournamentBonusPickValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.SlotIndex).GreaterThanOrEqualTo(0).LessThan(8);
        RuleFor(x => x.PickValue).NotEmpty().MaximumLength(100);
    }
}
