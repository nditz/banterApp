using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Syncs match events and lineups for live and recently finished fixtures.
/// </summary>
public sealed class MatchDetailsSyncJob
{
    public const string JobId = "match-details-sync";
    private const string Provider = "api_football";

    private readonly ISportsDataEnrichment _enrichment;
    private readonly ISportsDataProvider _provider;
    private readonly AppDbContext _db;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<MatchDetailsSyncJob> _logger;

    public MatchDetailsSyncJob(
        ISportsDataEnrichment enrichment,
        ISportsDataProvider provider,
        AppDbContext db,
        SyncRunTracker tracker,
        ILogger<MatchDetailsSyncJob> logger)
    {
        _enrichment = enrichment;
        _provider = provider;
        _db = db;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;

        try
        {
            var live = await _provider.GetLiveFixturesAsync(cancellationToken);
            var results = await _provider.GetResultsAsync(cancellationToken);
            var targetIds = live.Concat(results)
                .Select(m => m.Id)
                .Where(id => id.StartsWith("apifb-", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Take(20)
                .ToList();

            foreach (var matchId in targetIds)
            {
                var events = await _enrichment.GetMatchEventsAsync(matchId, cancellationToken);
                foreach (var evt in events)
                {
                    var existing = await _db.MatchEvents.FirstOrDefaultAsync(
                        x => x.MatchId == matchId && x.ProviderEventId == evt.ProviderEventId,
                        cancellationToken);

                    if (existing is null)
                    {
                        _db.MatchEvents.Add(new MatchEvent
                        {
                            Id = Guid.NewGuid(),
                            MatchId = matchId,
                            Minute = evt.Minute,
                            Type = evt.Type,
                            TeamCode = evt.TeamCode,
                            PlayerName = evt.PlayerName,
                            Detail = evt.Detail,
                            Provider = Provider,
                            ProviderEventId = evt.ProviderEventId
                        });
                        created++;
                    }
                    else
                    {
                        existing.Minute = evt.Minute;
                        existing.Type = evt.Type;
                        existing.TeamCode = evt.TeamCode;
                        existing.PlayerName = evt.PlayerName;
                        existing.Detail = evt.Detail;
                        updated++;
                    }
                }

                var lineups = await _enrichment.GetMatchLineupsAsync(matchId, cancellationToken);
                if (lineups.Count > 0)
                {
                    var existingLineups = await _db.LineupPlayers
                        .Where(x => x.MatchId == matchId)
                        .ToListAsync(cancellationToken);
                    if (existingLineups.Count > 0)
                    {
                        _db.LineupPlayers.RemoveRange(existingLineups);
                    }

                    foreach (var player in lineups)
                    {
                        _db.LineupPlayers.Add(new LineupPlayer
                        {
                            Id = Guid.NewGuid(),
                            MatchId = matchId,
                            TeamCode = player.TeamCode,
                            ShirtNumber = player.ShirtNumber,
                            PlayerName = player.PlayerName,
                            Position = player.Position,
                            IsSubstitute = player.IsSubstitute,
                            Provider = Provider
                        });
                        created++;
                    }
                }
            }

            if (created > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
            _logger.LogInformation(
                "Match details sync: {Matches} matches, {Created} created, {Updated} updated.",
                targetIds.Count,
                created,
                updated);
        }
        catch (Exception ex)
        {
            await _tracker.CompleteAsync(run, created, updated, failed: 1, errorMessage: ex.Message, cancellationToken);
            throw;
        }
    }
}
