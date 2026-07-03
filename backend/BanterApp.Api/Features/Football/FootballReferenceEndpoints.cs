using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Football;

public static class FootballReferenceEndpoints
{
    public static IEndpointRouteBuilder MapFootballReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/football").WithTags("Football Reference");

        group.MapGet("/countries", GetCountries)
            .RequireRateLimiting(RateLimitPolicies.PublicSearch);
        group.MapGet("/players", GetPlayers)
            .RequireRateLimiting(RateLimitPolicies.PublicSearch);
        group.MapGet("/leaderboards/top-scorers", GetTopScorers)
            .RequireRateLimiting(RateLimitPolicies.PublicSearch);
        group.MapGet("/leaderboards/top-assists", GetTopAssists)
            .RequireRateLimiting(RateLimitPolicies.PublicSearch);

        return app;
    }

    private static async Task<IResult> GetCountries(
        AppDbContext db,
        string? search,
        bool? includeInactive,
        CancellationToken ct)
    {
        var query = db.Countries.AsNoTracking().AsQueryable();
        if (includeInactive != true)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) ||
                                     (c.Code != null && c.Code.ToLower().Contains(term)));
        }

        var countries = await query
            .OrderBy(c => c.Name)
            .Select(c => new CountryResponse(
                c.Id,
                c.Name,
                c.Code,
                c.FlagUrl,
                c.Continent,
                c.FifaRanking,
                c.IsActive))
            .ToListAsync(ct);

        var deduped = countries
            .GroupBy(c => c.Code ?? c.Id.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderBy(c => c.FifaRanking ?? int.MaxValue)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Results.Ok(new CountriesListResponse(deduped));
    }

    private static async Task<IResult> GetPlayers(
        AppDbContext db,
        IOptions<FootballReferenceDataOptions> options,
        Guid? countryId,
        string? search,
        string? position,
        int? limit,
        CancellationToken ct)
    {
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var query = db.Players.AsNoTracking().Where(p => p.IsActive);

        if (countryId is not null)
        {
            query = query.Where(p => p.CountryId == countryId);
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            var pos = position.Trim();
            query = query.Where(p => p.Position != null && p.Position.Contains(pos));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.DisplayName.ToLower().Contains(term) ||
                (p.KnownName != null && p.KnownName.ToLower().Contains(term)) ||
                (p.ClubName != null && p.ClubName.ToLower().Contains(term)));
        }

        var comp = options.Value.CompetitionCode;
        var season = options.Value.Season;

        var players = await query
            .OrderBy(p => p.DisplayName)
            .Take(take)
            .Select(p => new PlayerResponse(
                p.Id,
                p.DisplayName,
                p.KnownName,
                p.Position,
                p.PhotoUrl,
                p.ClubName,
                p.CountryId,
                p.Country != null ? p.Country.Name : p.NationalTeamName,
                p.Country != null ? p.Country.Code : null,
                p.Country != null ? p.Country.FlagUrl : null,
                p.Stats
                    .Where(s => s.Competition == comp && s.Season == season)
                    .Select(s => new PlayerStatsSummary(s.Goals, s.Assists, s.MatchesPlayed, s.Rating))
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return Results.Ok(new PlayersListResponse(players));
    }

    private static async Task<IResult> GetTopScorers(
        AppDbContext db,
        IOptions<FootballReferenceDataOptions> options,
        CancellationToken ct)
    {
        return await GetLeaderboardAsync(db, options, LeaderboardTypes.TopScorers, ct);
    }

    private static async Task<IResult> GetTopAssists(
        AppDbContext db,
        IOptions<FootballReferenceDataOptions> options,
        CancellationToken ct)
    {
        return await GetLeaderboardAsync(db, options, LeaderboardTypes.TopAssists, ct);
    }

    private static async Task<IResult> GetLeaderboardAsync(
        AppDbContext db,
        IOptions<FootballReferenceDataOptions> options,
        string leaderboardType,
        CancellationToken ct)
    {
        var comp = options.Value.CompetitionCode;
        var season = options.Value.Season;

        var entries = await db.LeaderboardEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardType == leaderboardType &&
                        e.Competition == comp &&
                        e.Season == season)
            .OrderBy(e => e.Rank ?? int.MaxValue)
            .ThenByDescending(e => e.Value)
            .Take(50)
            .Select(e => new LeaderboardEntryResponse(
                e.Rank,
                e.Value,
                e.Player.DisplayName,
                e.Player.PhotoUrl,
                e.Country != null ? e.Country.Name : e.Player.NationalTeamName,
                e.Country != null ? e.Country.Code : null,
                e.Country != null ? e.Country.FlagUrl : null,
                e.SourceProvider,
                e.SourceUpdatedAt))
            .ToListAsync(ct);

        return Results.Ok(new LeaderboardResponse(leaderboardType, comp, season, entries));
    }
}

public sealed record CountryResponse(
    Guid Id,
    string Name,
    string? Code,
    string? FlagUrl,
    string? Continent,
    int? FifaRanking,
    bool IsActive);

public sealed record CountriesListResponse(IReadOnlyList<CountryResponse> Countries);

public sealed record PlayerStatsSummary(int Goals, int Assists, int MatchesPlayed, decimal? Rating);

public sealed record PlayerResponse(
    Guid Id,
    string DisplayName,
    string? KnownName,
    string? Position,
    string? PhotoUrl,
    string? ClubName,
    Guid? CountryId,
    string? CountryName,
    string? CountryCode,
    string? CountryFlagUrl,
    PlayerStatsSummary? Stats);

public sealed record PlayersListResponse(IReadOnlyList<PlayerResponse> Players);

public sealed record LeaderboardEntryResponse(
    int? Rank,
    decimal Value,
    string PlayerName,
    string? PhotoUrl,
    string? CountryName,
    string? CountryCode,
    string? CountryFlagUrl,
    string? SourceProvider,
    DateTimeOffset? SourceUpdatedAt);

public sealed record LeaderboardResponse(
    string LeaderboardType,
    string Competition,
    string Season,
    IReadOnlyList<LeaderboardEntryResponse> Entries);
