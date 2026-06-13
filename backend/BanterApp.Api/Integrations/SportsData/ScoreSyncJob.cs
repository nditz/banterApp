using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using Hangfire;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Hangfire recurring job that polls the configured sports data provider
/// and upserts fixtures/scores into the database.
/// </summary>
public sealed class ScoreSyncJob
{
    public const string JobId = "score-sync";
    private const string Provider = "api_football";

    private readonly ISportsDataProvider _provider;
    private readonly IEnumerable<ISportsDataFallbackProvider> _fallbacks;
    private readonly AppDbContext _db;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<ScoreSyncJob> _logger;

    public ScoreSyncJob(
        ISportsDataProvider provider,
        IEnumerable<ISportsDataFallbackProvider> fallbacks,
        AppDbContext db,
        SyncRunTracker tracker,
        ILogger<ScoreSyncJob> logger)
    {
        _provider = provider;
        _fallbacks = fallbacks;
        _db = db;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var added = 0;
        var updated = 0;

        try
        {
            var all = await _provider.GetAllFixturesAsync(cancellationToken);
            var live = await _provider.GetLiveFixturesAsync(cancellationToken);

            if (all.Count == 0)
            {
                foreach (var fallback in _fallbacks.Where(f => f.IsConfigured))
                {
                    var fallbackFixtures = await fallback.GetFixturesAsync(cancellationToken);
                    if (fallbackFixtures.Count > 0)
                    {
                        all = fallbackFixtures;
                        await _tracker.LogErrorAsync(
                            Provider,
                            JobId,
                            "fixture",
                            $"Canonical fixtures empty; used fallback provider {fallback.ProviderName}.",
                            run.Id,
                            ct: cancellationToken);
                        break;
                    }
                }
            }

            var merged = all
                .Concat(live)
                .GroupBy(d => d.Id)
                .Select(g => g.Last())
                .ToList();

            foreach (var dto in merged)
            {
                var match = await _db.Matches.FindAsync([dto.Id], cancellationToken);
                if (match is null)
                {
                    _db.Matches.Add(MatchMapper.FromDto(dto));
                    added++;
                }
                else if (MatchMapper.ApplyDto(match, dto))
                {
                    updated++;
                }

                if (dto.Id.StartsWith("apifb-", StringComparison.OrdinalIgnoreCase))
                {
                    var externalId = dto.Id["apifb-".Length..];
                    await _tracker.UpsertExternalIdAsync(
                        "fixture",
                        dto.Id,
                        Provider,
                        externalId,
                        ct: cancellationToken);
                }
            }

            if (added > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _tracker.CompleteAsync(run, added, updated, ct: cancellationToken);
            _logger.LogInformation(
                "Score sync: {Total} fixtures ({Live} live, {Added} added, {Updated} updated).",
                merged.Count,
                live.Count,
                added,
                updated);
        }
        catch (Exception ex)
        {
            await _tracker.CompleteAsync(run, added, updated, failed: 1, errorMessage: ex.Message, cancellationToken);
            throw;
        }
    }
}
