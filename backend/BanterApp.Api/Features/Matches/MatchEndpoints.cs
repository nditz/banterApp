using BanterApp.Api.Data;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Matches;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches");

        group.MapGet("/", GetAllMatches);
        group.MapGet("/upcoming", GetUpcomingMatches);
        group.MapGet("/results", GetMatchResults);
        group.MapGet("/{matchId}", GetMatchById);

        return app;
    }

    private static async Task<IResult> GetAllMatches(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches.OrderBy(m => m.KickoffTime).ToListAsync(ct);
        if (matches.Count == 0)
        {
            var upcoming = await sports.GetUpcomingFixturesAsync(ct);
            var results = await sports.GetResultsAsync(ct);
            return Results.Ok(upcoming.Concat(results).Select(MapFromDto).OrderBy(m => m.KickoffTime));
        }

        return Results.Ok(matches.Select(MapFromEntity));
    }

    private static async Task<IResult> GetUpcomingMatches(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches
            .Where(m => m.Status == "NS" || m.Status == "TBD" || m.Status == "Scheduled")
            .OrderBy(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            var upcoming = await sports.GetUpcomingFixturesAsync(ct);
            return Results.Ok(upcoming.Select(MapFromDto));
        }

        return Results.Ok(matches.Select(MapFromEntity));
    }

    private static async Task<IResult> GetMatchResults(AppDbContext db, ISportsDataProvider sports, CancellationToken ct)
    {
        var matches = await db.Matches
            .Where(m => m.Status == "FT")
            .OrderByDescending(m => m.KickoffTime)
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            var results = await sports.GetResultsAsync(ct);
            return Results.Ok(results.Select(MapFromDto));
        }

        return Results.Ok(matches.Select(MapFromEntity));
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
        if (entity is not null)
        {
            return Results.Ok(MapFromEntity(entity));
        }

        var upcoming = await sports.GetUpcomingFixturesAsync(ct);
        var match = upcoming.FirstOrDefault(m =>
            string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return Results.Ok(MapFromDto(match));
        }

        var results = await sports.GetResultsAsync(ct);
        match = results.FirstOrDefault(m =>
            string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return Results.Ok(MapFromDto(match));
        }

        return Results.NotFound(new { error = "Match not found." });
    }

    private static MatchResponse MapFromEntity(Data.Entities.Match m) =>
        new(m.Id, m.TeamA, m.TeamB, m.TeamACode, m.TeamBCode, m.KickoffTime, m.Stage, m.Group, m.Venue, m.Status, m.HomeScore, m.AwayScore);

    private static MatchResponse MapFromDto(Integrations.SportsData.Dtos.MatchDto m) =>
        new(m.Id, m.HomeTeam.Name, m.AwayTeam.Name, m.HomeTeam.Code, m.AwayTeam.Code, m.KickoffUtc, m.Stage, m.Group, m.Venue, m.Status, m.HomeScore, m.AwayScore);
}
