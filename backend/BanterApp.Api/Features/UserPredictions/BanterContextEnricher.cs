using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.UserPredictions;
using BanterApp.Api.Integrations.FootballReference;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.UserPredictions;

public sealed class BanterContextEnricher(
    AppDbContext db,
    UserPredictionAggregateService aggregates,
    IOptions<FootballReferenceDataOptions> options)
{
    public async Task<string> BuildContextJsonAsync(CancellationToken cancellationToken = default)
    {
        var comp = options.Value.CompetitionCode;
        var season = options.Value.Season;

        var topScorers = await db.LeaderboardEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardType == LeaderboardTypes.TopScorers &&
                        e.Competition == comp && e.Season == season)
            .OrderBy(e => e.Rank)
            .Take(5)
            .Select(e => new
            {
                player = e.Player.DisplayName,
                country = e.Country != null ? e.Country.Name : e.Player.NationalTeamName,
                goals = e.Value,
                rank = e.Rank
            })
            .ToListAsync(cancellationToken);

        var topAssists = await db.LeaderboardEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardType == LeaderboardTypes.TopAssists &&
                        e.Competition == comp && e.Season == season)
            .OrderBy(e => e.Rank)
            .Take(5)
            .Select(e => new
            {
                player = e.Player.DisplayName,
                country = e.Country != null ? e.Country.Name : e.Player.NationalTeamName,
                assists = e.Value,
                rank = e.Rank
            })
            .ToListAsync(cancellationToken);

        var playerStats = await db.PlayerStats
            .AsNoTracking()
            .Where(s => s.Competition == comp && s.Season == season)
            .OrderByDescending(s => s.Goals)
            .Take(5)
            .Select(s => new
            {
                player = s.Player.DisplayName,
                country = s.Country != null ? s.Country.Name : s.Player.NationalTeamName,
                s.Goals,
                s.Assists,
                s.MatchesPlayed
            })
            .ToListAsync(cancellationToken);

        var winnerAgg = await aggregates.GetAggregatesAsync(
            UserPredictionTypes.LeagueWinner, comp, season, cancellationToken);
        var scorerAgg = await aggregates.GetAggregatesAsync(
            UserPredictionTypes.TopGoalScorer, comp, season, cancellationToken);
        var bestPlayerAgg = await aggregates.GetAggregatesAsync(
            UserPredictionTypes.BestPlayer, comp, season, cancellationToken);

        var context = new
        {
            top_user_predictions = new[]
            {
                new { type = UserPredictionTypes.LeagueWinner, entries = winnerAgg.Entries.Take(3) },
                new { type = UserPredictionTypes.TopGoalScorer, entries = scorerAgg.Entries.Take(3) },
                new { type = UserPredictionTypes.BestPlayer, entries = bestPlayerAgg.Entries.Take(3) }
            },
            top_scorers = topScorers,
            top_assists = topAssists,
            player_stats = playerStats,
            country_stats = winnerAgg.Entries.Select(e => new
            {
                country = e.Name,
                prediction_count = e.PredictionCount,
                percentage = e.Percentage
            })
        };

        return JsonSerializer.Serialize(context);
    }
}
