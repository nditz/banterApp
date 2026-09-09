using BanterApp.Api.Data;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Matches;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var matches = app.MapGroup("/api/matches").WithTags("Matches");
        matches.MapGet("/", GetAllMatches);
        matches.MapGet("/upcoming", GetUpcomingMatches);
        matches.MapGet("/results", GetMatchResults);
        matches.MapGet("/matchweek/{number:int}", GetMatchweekFixtures);
        matches.MapGet("/{matchId}", GetMatchById);

        var weeks = app.MapGroup("/api/matchweeks").WithTags("Matchweeks");
        weeks.MapGet("/", GetMatchweeks);
        weeks.MapGet("/current", GetCurrentMatchweek);

        app.MapGet("/api/standings", GetStandings).WithTags("Standings");

        return app;
    }

    private static async Task<IResult> GetAllMatches(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches
            .WherePremierLeague()
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);
        if (matches.Count > 0)
        {
            return Results.Ok(matches.Select(MapFromEntity));
        }

        var fromProvider = await TryMapProviderFixturesAsync(
            async token =>
            {
                var upcoming = await sports.GetUpcomingFixturesAsync(token);
                var results = await sports.GetResultsAsync(token);
                return upcoming.Concat(results);
            },
            ct);
        return Results.Ok(fromProvider.OrderBy(m => m.KickoffTime));
    }

    private static async Task<IResult> GetUpcomingMatches(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-3);
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.Status == "NS" || m.Status == "TBD" || m.Status == "Scheduled")
            .Where(m => m.KickoffTime > cutoff)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count > 0)
        {
            return Results.Ok(matches.Select(MapFromEntity));
        }

        // Stored PL rows are canonical. Do not live-substitute NS fixtures when the
        // database already has a (possibly stale/overdue) calendar.
        if (await HasStoredPremierLeagueMatchesAsync(db, ct))
        {
            return Results.Ok(Array.Empty<MatchResponse>());
        }

        return Results.Ok(await TryMapProviderFixturesAsync(sports.GetUpcomingFixturesAsync, ct));
    }

    private static async Task<IResult> GetMatchResults(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.Status == "FT" || m.Status == "AET" || m.Status == "PEN")
            .OrderByDescending(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count > 0)
        {
            return Results.Ok(matches.Select(MapFromEntity));
        }

        if (await HasStoredPremierLeagueMatchesAsync(db, ct))
        {
            return Results.Ok(Array.Empty<MatchResponse>());
        }

        return Results.Ok(await TryMapProviderFixturesAsync(sports.GetResultsAsync, ct));
    }

    private static async Task<IResult> GetMatchweekFixtures(
        int number,
        AppDbContext db,
        ISportsDataProvider sports,
        CancellationToken ct)
    {
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.MatchweekNumber == number)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count > 0)
        {
            return Results.Ok(matches.Select(MapFromEntity));
        }

        if (await HasStoredPremierLeagueMatchesAsync(db, ct))
        {
            return Results.Ok(Array.Empty<MatchResponse>());
        }

        var fromProvider = await TryMapProviderFixturesAsync(sports.GetAllFixturesAsync, ct);
        return Results.Ok(fromProvider.Where(m => m.MatchweekNumber == number));
    }

    private static async Task<IResult> GetMatchweeks(AppDbContext db, CancellationToken ct)
    {
        var current = await ResolveCurrentMatchweekNumberAsync(db, ct);
        var rows = await db.Matches
            .WherePremierLeague()
            .Where(m => m.MatchweekNumber != null)
            .Select(m => new { m.MatchweekNumber, m.Status, m.KickoffTime })
            .ToListAsync(ct);

        var weeks = rows
            .GroupBy(m => m.MatchweekNumber!.Value)
            .Select(g => new MatchweekResponse(
                g.Key,
                $"Matchweek {g.Key}",
                g.All(m => CurrentMatchweek.IsFinished(m.Status)) ? "complete" : "open",
                g.Min(m => m.KickoffTime),
                g.Max(m => m.KickoffTime),
                g.Count(),
                0,
                g.Key == current))
            .OrderBy(w => w.Number)
            .ToList();

        return Results.Ok(weeks);
    }

    private static async Task<IResult> GetCurrentMatchweek(
        AppDbContext db,
        ISportsDataProvider sports,
        IOptions<SportsDataOptions> sportsOptions,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var isMock = FootballDatasetStatus.IsMockProvider(sportsOptions.Value.Provider);
        var dbMatches = await db.Matches
            .WherePremierLeague()
            .Select(m => new { m.Id, m.MatchweekNumber, m.Status, m.KickoffTime })
            .ToListAsync(ct);

        if (dbMatches.Count == 0)
        {
            try
            {
                var all = FilterPremierLeagueDtos(await sports.GetAllFixturesAsync(ct)).ToList();
                var number = CurrentMatchweek.Resolve(
                    all.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffUtc)),
                    now);
                var fromProvider = all
                    .Where(m => m.MatchweekNumber == number)
                    .OrderBy(m => m.KickoffUtc)
                    .Select(MapFromDto)
                    .ToList();
                var overdue = FootballDatasetStatus.HasOverdueUnfinished(
                    fromProvider.Select(m => ((string?)m.Status, m.KickoffTime)),
                    now);
                var status = FootballDatasetStatus.FromFixtures(fromProvider.Count, overdue, providerFailed: false);
                return Results.Ok(new CurrentMatchweekApiResponse(
                    number,
                    fromProvider,
                    status,
                    isMock ? "mock" : "provider",
                    Official: FootballDatasetStatus.LooksLikeOfficialIds(fromProvider.Select(m => m.Id)),
                    Error: status == FootballDatasetStatus.Stale
                        ? "These fixtures are overdue without results. Score sync may be failing."
                        : null));
            }
            catch (Exception)
            {
                return Results.Ok(new CurrentMatchweekApiResponse(
                    0,
                    [],
                    FootballDatasetStatus.Error,
                    isMock ? "mock" : "provider",
                    Official: false,
                    Error: "Current matchweek fixtures could not be loaded from the sports provider."));
            }
        }

        var numberFromDb = CurrentMatchweek.Resolve(
            dbMatches.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffTime)),
            now);
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.MatchweekNumber == numberFromDb)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);
        var mapped = matches.Select(MapFromEntity).ToList();
        var overdueDb = FootballDatasetStatus.HasOverdueUnfinished(
            mapped.Select(m => ((string?)m.Status, m.KickoffTime)),
            now);
        var looksMock = FootballDatasetStatus.LooksLikeMockIds(mapped.Select(m => m.Id));
        var statusDb = FootballDatasetStatus.FromFixtures(mapped.Count, overdueDb, providerFailed: false);
        return Results.Ok(new CurrentMatchweekApiResponse(
            numberFromDb,
            mapped,
            statusDb,
            looksMock ? "mock" : "database",
            Official: FootballDatasetStatus.LooksLikeOfficialIds(mapped.Select(m => m.Id)),
            Error: statusDb == FootballDatasetStatus.Stale
                ? "These fixtures are overdue without results. Score sync may be failing."
                : null));
    }

    private static async Task<IResult> GetStandings(
        AppDbContext db,
        ISportsDataProvider sports,
        IOptions<SportsDataOptions> sportsOptions,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var isMock = FootballDatasetStatus.IsMockProvider(sportsOptions.Value.Provider);
        var plMatches = await db.Matches.WherePremierLeague().ToListAsync(ct);
        var overdue = FootballDatasetStatus.HasOverdueUnfinished(
            plMatches.Select(m => ((string?)m.Status, m.KickoffTime)),
            now);
        var lastSyncedAt = await db.StandingRows
            .AsNoTracking()
            .Where(r => r.GroupKey == "PL")
            .OrderByDescending(r => r.LastSyncedAt)
            .Select(r => (DateTimeOffset?)r.LastSyncedAt)
            .FirstOrDefaultAsync(ct);

        try
        {
            var computed = PremierLeagueStandingsCalculator.FromMatches(plMatches);
            if (computed.Count > 0 && computed.Any(r => r.Played > 0))
            {
                var status = overdue ? FootballDatasetStatus.Stale : FootballDatasetStatus.Ok;
                return Results.Ok(new StandingsApiResponse(
                    status,
                    "computed",
                    lastSyncedAt,
                    status == FootballDatasetStatus.Stale
                        ? "Standings are based on finished matches, but some kickoffs are overdue without results."
                        : null,
                    computed));
            }

            var rows = await db.StandingRows
                .Where(r => r.GroupKey == "PL")
                .ToListAsync(ct);

            if (rows.Count == 0)
            {
                var standings = await sports.GetStandingsAsync("PL", ct);
                var ranked = PremierLeagueTableRanking.Rank(standings.Select(r => new StandingRowResponse(
                    r.Rank, r.Team.Code, r.Team.Name, ClubBadges.Coalesce(r.Team.LogoUrl, r.Team.Code, r.Team.Name), r.Played, r.Won, r.Drawn, r.Lost, r.GoalsFor, r.GoalsAgainst, r.GoalDifference, r.Points)));
                var status = ranked.Count == 0
                    ? FootballDatasetStatus.Empty
                    : overdue ? FootballDatasetStatus.Stale : FootballDatasetStatus.Ok;
                return Results.Ok(new StandingsApiResponse(
                    status,
                    isMock ? "mock" : "provider",
                    lastSyncedAt,
                    status == FootballDatasetStatus.Empty
                        ? "No Premier League standings yet."
                        : status == FootballDatasetStatus.Stale
                            ? "Standings may be behind — some fixtures are overdue without results."
                            : null,
                    ranked));
            }

            var latestByTeam = rows
                .GroupBy(r => r.TeamCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.LastSyncedAt).First())
                .Select(r => new StandingRowResponse(
                    r.Rank, r.TeamCode, r.TeamName, ClubBadges.Coalesce(r.LogoUrl, r.TeamCode, r.TeamName), r.Played, r.Won, r.Drawn, r.Lost, r.GoalsFor, r.GoalsAgainst, r.GoalDiff, r.Points));
            var cached = PremierLeagueTableRanking.Rank(latestByTeam);
            var cachedStatus = cached.Count == 0
                ? FootballDatasetStatus.Empty
                : overdue ? FootballDatasetStatus.Stale : FootballDatasetStatus.Ok;
            return Results.Ok(new StandingsApiResponse(
                cachedStatus,
                "cache",
                lastSyncedAt,
                cachedStatus == FootballDatasetStatus.Stale
                    ? "Standings may be behind — some fixtures are overdue without results."
                    : null,
                cached));
        }
        catch (Exception)
        {
            return Results.Ok(new StandingsApiResponse(
                FootballDatasetStatus.Error,
                isMock ? "mock" : "provider",
                lastSyncedAt,
                "Standings could not be loaded from the sports provider.",
                []));
        }
    }

    private static async Task<IResult> GetMatchById(
        string matchId,
        AppDbContext db,
        ISportsDataProvider sports,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return Results.BadRequest(new { error = "Match id is required." });
        }

        var entity = await db.Matches.FindAsync([matchId], ct);
        if (entity is not null && PremierLeagueMatchScope.IsPremierLeague(entity))
        {
            return Results.Ok(MapFromEntity(entity));
        }

        if (await HasStoredPremierLeagueMatchesAsync(db, ct))
        {
            return Results.NotFound(new { error = "Match not found." });
        }

        try
        {
            var upcoming = FilterPremierLeagueDtos(await sports.GetUpcomingFixturesAsync(ct));
            var match = upcoming.FirstOrDefault(m =>
                string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return Results.Ok(MapFromDto(match));
            }

            var results = FilterPremierLeagueDtos(await sports.GetResultsAsync(ct));
            match = results.FirstOrDefault(m =>
                string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return Results.Ok(MapFromDto(match));
            }
        }
        catch (Exception)
        {
            return Results.NotFound(new { error = "Match not found." });
        }

        return Results.NotFound(new { error = "Match not found." });
    }

    private static Task<bool> HasStoredPremierLeagueMatchesAsync(AppDbContext db, CancellationToken ct) =>
        db.Matches.WherePremierLeague().AnyAsync(ct);

    private static async Task<List<MatchResponse>> TryMapProviderFixturesAsync(
        Func<CancellationToken, Task<IEnumerable<Integrations.SportsData.Dtos.MatchDto>>> fetch,
        CancellationToken ct)
    {
        try
        {
            return FilterPremierLeagueDtos(await fetch(ct)).Select(MapFromDto).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static async Task<List<MatchResponse>> TryMapProviderFixturesAsync(
        Func<CancellationToken, Task<IReadOnlyList<Integrations.SportsData.Dtos.MatchDto>>> fetch,
        CancellationToken ct)
    {
        return await TryMapProviderFixturesAsync(
            async token => (IEnumerable<Integrations.SportsData.Dtos.MatchDto>)await fetch(token),
            ct);
    }

    private static IEnumerable<Integrations.SportsData.Dtos.MatchDto> FilterPremierLeagueDtos(
        IEnumerable<Integrations.SportsData.Dtos.MatchDto> fixtures) =>
        fixtures.Where(PremierLeagueMatchScope.IsPremierLeagueDto);

    public static async Task<int> ResolveCurrentMatchweekNumberAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = await db.Matches
            .WherePremierLeague()
            .Select(m => new { m.MatchweekNumber, m.Status, m.KickoffTime })
            .ToListAsync(ct);

        return CurrentMatchweek.Resolve(
            rows.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffTime)),
            DateTimeOffset.UtcNow);
    }

    private static MatchResponse MapFromEntity(Data.Entities.Match m) =>
        new(m.Id, m.TeamA, m.TeamB, m.TeamACode, m.TeamBCode,
            ClubBadges.Coalesce(m.HomeLogoUrl, m.TeamACode, m.TeamA),
            ClubBadges.Coalesce(m.AwayLogoUrl, m.TeamBCode, m.TeamB),
            m.KickoffTime, m.Stage, m.Group, m.MatchweekNumber, m.Venue, m.Status, m.HomeScore, m.AwayScore, MatchLockService.IsLocked(m));

    private static MatchResponse MapFromDto(Integrations.SportsData.Dtos.MatchDto m) =>
        new(m.Id, m.HomeTeam.Name, m.AwayTeam.Name, m.HomeTeam.Code, m.AwayTeam.Code,
            ClubBadges.Coalesce(m.HomeTeam.LogoUrl, m.HomeTeam.Code, m.HomeTeam.Name),
            ClubBadges.Coalesce(m.AwayTeam.LogoUrl, m.AwayTeam.Code, m.AwayTeam.Name),
            m.KickoffUtc, m.Stage, m.Group, m.MatchweekNumber, m.Venue, m.Status, m.HomeScore, m.AwayScore, m.KickoffUtc <= DateTimeOffset.UtcNow || m.Status == "FT");
}
