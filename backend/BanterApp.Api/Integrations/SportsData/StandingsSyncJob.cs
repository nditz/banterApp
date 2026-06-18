using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Persists tournament standings from the canonical sports data provider.
/// </summary>
public sealed class StandingsSyncJob
{
    public const string JobId = "standings-sync";
    private const string Provider = "api_football";

    private readonly ISportsDataEnrichment _enrichment;
    private readonly IEnumerable<ISportsDataFallbackProvider> _fallbacks;
    private readonly AppDbContext _db;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<StandingsSyncJob> _logger;

    public StandingsSyncJob(
        ISportsDataEnrichment enrichment,
        IEnumerable<ISportsDataFallbackProvider> fallbacks,
        AppDbContext db,
        SyncRunTracker tracker,
        ILogger<StandingsSyncJob> logger)
    {
        _enrichment = enrichment;
        _db = db;
        _tracker = tracker;
        _logger = logger;
        _fallbacks = fallbacks;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;

        try
        {
            var standings = await _enrichment.GetAllStandingsAsync(cancellationToken);
            if (standings.Count == 0)
            {
                foreach (var fallback in _fallbacks.Where(f => f.IsConfigured))
                {
                    standings = await fallback.GetStandingsAsync(cancellationToken);
                    if (standings.Count > 0)
                    {
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "standings",
                            $"Canonical standings empty; used fallback provider {fallback.ProviderName}.",
                            run.Id,
                            ct: cancellationToken);
                        break;
                    }
                }
            }

            foreach (var (groupKey, rows) in standings)
            {
                foreach (var row in rows)
                {
                    var existing = await _db.StandingRows.FirstOrDefaultAsync(
                        x => x.GroupKey == groupKey &&
                             x.TeamCode == row.Team.Code &&
                             x.Provider == Provider,
                        cancellationToken);

                    if (existing is null)
                    {
                        _db.StandingRows.Add(new StandingRow
                        {
                            Id = Guid.NewGuid(),
                            GroupKey = groupKey,
                            Rank = row.Rank,
                            TeamCode = row.Team.Code,
                            TeamName = row.Team.Name,
                            Played = row.Played,
                            Won = row.Won,
                            Drawn = row.Drawn,
                            Lost = row.Lost,
                            GoalsFor = row.GoalsFor,
                            GoalsAgainst = row.GoalsAgainst,
                            GoalDiff = row.GoalDifference,
                            Points = row.Points,
                            Provider = Provider,
                            LastSyncedAt = DateTimeOffset.UtcNow
                        });
                        created++;
                    }
                    else
                    {
                        existing.Rank = row.Rank;
                        existing.TeamName = row.Team.Name;
                        existing.Played = row.Played;
                        existing.Won = row.Won;
                        existing.Drawn = row.Drawn;
                        existing.Lost = row.Lost;
                        existing.GoalsFor = row.GoalsFor;
                        existing.GoalsAgainst = row.GoalsAgainst;
                        existing.GoalDiff = row.GoalDifference;
                        existing.Points = row.Points;
                        existing.LastSyncedAt = DateTimeOffset.UtcNow;
                        updated++;
                    }

                    await _tracker.UpsertExternalIdAsync(
                        "team",
                        row.Team.Code,
                        Provider,
                        row.Team.Id,
                        ct: cancellationToken);
                }
            }

            if (created > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
            _logger.LogInformation("Standings sync: {Groups} groups, {Created} created, {Updated} updated.",
                standings.Count, created, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Standings sync job failed.");
            await _tracker.FailAsync(run, created, updated, ex, cancellationToken);
        }
    }
}
