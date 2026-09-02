using BanterApp.Api.Data;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.SportsData;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Sync;

public static class SyncEndpoints
{
    public static void MapSyncEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sync")
            .WithTags("Sync")
            .RequireAuthorization("Admin");

        group.MapGet("/runs", async (AppDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            var runs = await db.SyncRuns
                .OrderByDescending(r => r.StartedAt)
                .Take(take)
                .Select(r => new
                {
                    r.Id,
                    r.Provider,
                    r.JobName,
                    r.StartedAt,
                    r.FinishedAt,
                    r.Status,
                    r.RecordsCreated,
                    r.RecordsUpdated,
                    r.RecordsFailed,
                    r.ErrorMessage
                })
                .ToListAsync(ct);

            return Results.Ok(runs);
        });

        group.MapGet("/errors", async (AppDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            var errors = await db.SyncErrors
                .OrderByDescending(e => e.OccurredAt)
                .Take(take)
                .Select(e => new
                {
                    e.Id,
                    e.SyncRunId,
                    e.Provider,
                    e.JobName,
                    e.EntityType,
                    e.EntityId,
                    e.Message,
                    e.OccurredAt
                })
                .ToListAsync(ct);

            return Results.Ok(errors);
        });

        group.MapGet("/application-errors", async (AppDbContext db, int? limit, CancellationToken ct) =>
        {
            var take = Math.Clamp(limit ?? 20, 1, 100);
            var errors = await db.ApplicationErrorLogs
                .OrderByDescending(e => e.OccurredAt)
                .Take(take)
                .Select(e => new
                {
                    e.Id,
                    e.Source,
                    e.Category,
                    e.Message,
                    e.RequestMethod,
                    e.RequestPath,
                    e.StatusCode,
                    e.SyncRunId,
                    e.OccurredAt
                })
                .ToListAsync(ct);

            return Results.Ok(errors);
        });

        group.MapGet("/status", async (
            AppDbContext db,
            IYouTubeProvider youtube,
            IEnumerable<ISportsDataFallbackProvider> fallbacks,
            CancellationToken ct) =>
        {
            var latestRuns = await db.SyncRuns
                .OrderByDescending(r => r.StartedAt)
                .Take(10)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                latestRuns,
                mediaSourceCount = await db.MediaSources.CountAsync(ct),
                mediaItemCount = await db.MediaItems.CountAsync(ct),
                standingRowCount = await db.StandingRows.CountAsync(ct),
                matchEventCount = await db.MatchEvents.CountAsync(ct),
                lineupPlayerCount = await db.LineupPlayers.CountAsync(ct),
                providers = new
                {
                    youtubeConfigured = youtube.IsConfigured,
                    fallbacks = fallbacks.Select(f => new { f.ProviderName, f.IsConfigured })
                }
            });
        });

        group.MapPost("/trigger/{jobName}", (string jobName) =>
        {
            var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
            var jobId = jobName.Trim().ToLowerInvariant() switch
            {
                "score-sync" => ScoreSyncJob.JobId,
                "standings-sync" => StandingsSyncJob.JobId,
                "match-details-sync" => MatchDetailsSyncJob.JobId,
                "news-ingest" => Integrations.News.NewsIngestJob.JobId,
                "media-ingest" => MediaIngestJob.JobId,
                "ai-reactions" => Integrations.Ai.AiReactionJob.JobId,
                "feed-banter-enrich" => Integrations.Ai.FeedBanterEnrichmentJob.JobId,
                "youtube-opinion-sync" => YouTubeSearchSyncJob.JobId,
                "rss-feed-resolve" => Integrations.Rss.RssFeedResolveJob.JobId,
                "rss-opinion-sync" => RssOpinionSyncJob.JobId,
                "pundit-content-enrich" => ContentEnrichmentJob.JobId,
                "pundit-extraction" => PunditExtractionJob.JobId,
                "prediction-aggregate-refresh" => PredictionAggregateJob.JobId,
                _ => null
            };

            if (jobId is null)
            {
                return Results.BadRequest(new { error = "Unknown job name." });
            }

            recurring.Trigger(jobId);
            return Results.Ok(new { triggered = jobId });
        });

        group.MapPost("/reset-demo-data", async (
            LiveDataResetService reset,
            IWebHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!env.IsDevelopment())
            {
                return Results.NotFound();
            }

            var result = await reset.ResetDemoDataAsync(ct);
            return Results.Ok(result);
        });

        group.MapPost("/refresh-all", async (
            LiveDataResetService reset,
            IWebHostEnvironment env,
            IRecurringJobManager recurring,
            CancellationToken ct) =>
        {
            if (!env.IsDevelopment())
            {
                return Results.NotFound();
            }

            var cleared = await reset.ResetDemoDataAsync(ct);

            foreach (var jobId in new[]
                     {
                         ScoreSyncJob.JobId,
                         StandingsSyncJob.JobId,
                         MatchDetailsSyncJob.JobId,
                         Integrations.News.NewsIngestJob.JobId,
                         MediaIngestJob.JobId,
                         Integrations.Ai.AiReactionJob.JobId,
                         Integrations.Ai.FeedBanterEnrichmentJob.JobId,
                         YouTubeSearchSyncJob.JobId,
                         Integrations.Rss.RssFeedResolveJob.JobId,
                         RssOpinionSyncJob.JobId,
                         ContentEnrichmentJob.JobId,
                         PunditExtractionJob.JobId,
                         PredictionAggregateJob.JobId
                     })
            {
                recurring.Trigger(jobId);
            }

            return Results.Ok(new
            {
                cleared,
                message = "Demo data cleared; live ingest jobs triggered. Wait ~30s then check /api/health and /api/feed."
            });
        });
    }
}
