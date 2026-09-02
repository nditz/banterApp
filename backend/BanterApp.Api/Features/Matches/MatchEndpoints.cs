using BanterApp.Api.Data;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

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
        if (matches.Count == 0)
        {
            var upcoming = await sports.GetUpcomingFixturesAsync(ct);
            var results = await sports.GetResultsAsync(ct);
            return Results.Ok(FilterPremierLeagueDtos(upcoming.Concat(results))
                .Select(MapFromDto)
                .OrderBy(m => m.KickoffTime));
        }

        return Results.Ok(matches.Select(MapFromEntity));
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

        if (matches.Count == 0)
        {
            var upcoming = await sports.GetUpcomingFixturesAsync(ct);
            return Results.Ok(FilterPremierLeagueDtos(upcoming).Select(MapFromDto));
        }

        return Results.Ok(matches.Select(MapFromEntity));
    }

    private static async Task<IResult> GetMatchResults(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.Status == "FT" || m.Status == "AET" || m.Status == "PEN")
            .OrderByDescending(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            var results = await sports.GetResultsAsync(ct);
            return Results.Ok(FilterPremierLeagueDtos(results).Select(MapFromDto));
        }

        return Results.Ok(matches.Select(MapFromEntity));
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

        if (matches.Count == 0)
        {
            var all = await sports.GetAllFixturesAsync(ct);
            return Results.Ok(FilterPremierLeagueDtos(all)
                .Where(m => m.MatchweekNumber == number)
                .Select(MapFromDto));
        }

        return Results.Ok(matches.Select(MapFromEntity));
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

    private static async Task<IResult> GetCurrentMatchweek(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var dbMatches = await db.Matches
            .WherePremierLeague()
            .Select(m => new { m.MatchweekNumber, m.Status, m.KickoffTime })
            .ToListAsync(ct);

        int number;
        if (dbMatches.Count == 0)
        {
            var all = FilterPremierLeagueDtos(await sports.GetAllFixturesAsync(ct)).ToList();
            number = CurrentMatchweek.Resolve(
                all.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffUtc)),
                DateTimeOffset.UtcNow);
            var fromProvider = all
                .Where(m => m.MatchweekNumber == number)
                .OrderBy(m => m.KickoffUtc)
                .Select(MapFromDto)
                .ToList();
            return Results.Ok(new { number, matches = fromProvider });
        }

        number = CurrentMatchweek.Resolve(
            dbMatches.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffTime)),
            DateTimeOffset.UtcNow);
        var matches = await db.Matches
            .WherePremierLeague()
            .Where(m => m.MatchweekNumber == number)
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);

        return Results.Ok(new
        {
            number,
            matches = matches.Select(MapFromEntity)
        });
    }

    private static async Task<IResult> GetStandings(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var plMatches = await db.Matches.WherePremierLeague().ToListAsync(ct);
        var computed = PremierLeagueStandingsCalculator.FromMatches(plMatches);
        if (computed.Count > 0 && computed.Any(r => r.Played > 0))
        {
            return Results.Ok(computed);
        }

        var rows = await db.StandingRows
            .Where(r => r.GroupKey == "PL")
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            var standings = await sports.GetStandingsAsync("PL", ct);
            return Results.Ok(PremierLeagueTableRanking.Rank(standings.Select(r => new StandingRowResponse(
                r.Rank, r.Team.Code, r.Team.Name, ClubBadges.Coalesce(r.Team.LogoUrl, r.Team.Code, r.Team.Name), r.Played, r.Won, r.Drawn, r.Lost, r.GoalsFor, r.GoalsAgainst, r.GoalDifference, r.Points))));
        }

        var latestByTeam = rows
            .GroupBy(r => r.TeamCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.LastSyncedAt).First())
            .Select(r => new StandingRowResponse(
                r.Rank, r.TeamCode, r.TeamName, ClubBadges.Coalesce(r.LogoUrl, r.TeamCode, r.TeamName), r.Played, r.Won, r.Drawn, r.Lost, r.GoalsFor, r.GoalsAgainst, r.GoalDiff, r.Points));

        return Results.Ok(PremierLeagueTableRanking.Rank(latestByTeam));
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

        return Results.NotFound(new { error = "Match not found." });
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
