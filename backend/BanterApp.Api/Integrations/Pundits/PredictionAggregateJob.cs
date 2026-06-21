using BanterApp.Api.Integrations.Common;
using Hangfire;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PredictionAggregateJob
{
    public const string JobId = "prediction-aggregate-refresh";
    private const string Provider = "pundit-aggregate";

    private readonly PredictionAggregateService _aggregates;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<PredictionAggregateJob> _logger;

    public PredictionAggregateJob(
        PredictionAggregateService aggregates,
        SyncRunTracker tracker,
        ILogger<PredictionAggregateJob> logger)
    {
        _aggregates = aggregates;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);

        try
        {
            var updated = await _aggregates.RefreshAsync(cancellationToken);
            await _tracker.CompleteAsync(run, updated, 0, 0, ct: cancellationToken);
            _logger.LogInformation("Prediction aggregates refreshed: {Count} groups.", updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prediction aggregate refresh failed.");
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
        }
    }
}
