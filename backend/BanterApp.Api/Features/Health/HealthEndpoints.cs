using BanterApp.Api.Data;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Health;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapGet("/api/health", async (
            AppDbContext db,
            IOptions<SportsDataOptions> sportsOptions,
            IOptions<FootballDataOptions> footballDataOptions,
            CancellationToken ct) =>
        {
            var sports = sportsOptions.Value;
            var hasSportsKey = !string.IsNullOrWhiteSpace(sports.ApiKey);
            var hasFootballDataToken = !string.IsNullOrWhiteSpace(footballDataOptions.Value.Token);
            var sportsMode =
                FootballDatasetStatus.IsFootballDataProvider(sports.Provider)
                    ? hasFootballDataToken ? "football-data-live" : "football-data-unconfigured"
                    : sports.Provider switch
                    {
                        "apifootball" when hasSportsKey => "apifootball-live",
                        "apifootball" => "apifootball-mock-fallback",
                        _ => "mock"
                    };

            try
            {
                var canConnect = await db.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    return Results.Json(
                        new { status = "degraded", database = new { connected = false, provider = "unknown" } },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
                var matchCount = await db.Matches.CountAsync(ct);
                var newsCount = await db.NewsFeedItems.CountAsync(ct);
                var lastScoreSync = await db.SyncRuns
                    .Where(r => r.JobName == ScoreSyncJob.JobId)
                    .OrderByDescending(r => r.StartedAt)
                    .Select(r => new { r.Status, r.FinishedAt, r.ErrorMessage, r.ItemsProcessed })
                    .FirstOrDefaultAsync(ct);
                var overdueUnfinished = await db.Matches
                    .WherePremierLeague()
                    .AnyAsync(
                        m => m.KickoffTime <= DateTimeOffset.UtcNow &&
                             m.Status != "FT" &&
                             m.Status != "AET" &&
                             m.Status != "PEN" &&
                             m.Status != "WO" &&
                             m.Status != "CANC" &&
                             m.Status != "ABD" &&
                             m.Status != "LIVE" &&
                             m.Status != "1H" &&
                             m.Status != "2H" &&
                             m.Status != "HT",
                        ct);

                var providerUnconfigured = sportsMode is "football-data-unconfigured" or "apifootball-mock-fallback";
                return Results.Ok(new
                {
                    status = overdueUnfinished || providerUnconfigured ? "degraded" : "ok",
                    database = new
                    {
                        connected = true,
                        provider = isPostgres ? "postgresql" : "inmemory",
                        matchCount,
                        newsCount
                    },
                    sportsData = new
                    {
                        provider = sports.Provider,
                        mode = sportsMode,
                        syncIntervalMinutes = sports.SyncIntervalMinutes,
                        lastScoreSyncStatus = lastScoreSync?.Status,
                        lastScoreSyncAt = lastScoreSync?.FinishedAt,
                        lastScoreSyncError = lastScoreSync?.ErrorMessage,
                        lastScoreSyncItems = lastScoreSync?.ItemsProcessed,
                        overdueUnfinishedFixtures = overdueUnfinished
                    }
                });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { status = "error", message = ex.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .AllowAnonymous()
        .ExcludeFromDescription();
    }
}
