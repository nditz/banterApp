using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Periodically syncs fixtures from the configured sports data provider.
/// Phase 1: log-only until the database layer is wired.
/// </summary>
public sealed class SportsDataSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SportsDataOptions _options;
    private readonly ILogger<SportsDataSyncService> _logger;

    public SportsDataSyncService(
        IServiceScopeFactory scopeFactory,
        IOptions<SportsDataOptions> options,
        ILogger<SportsDataSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sports data sync service started (provider: {Provider}, interval: {Interval} min).",
            _options.Provider,
            _options.SyncIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncOnceAsync(stoppingToken);

            var delay = TimeSpan.FromMinutes(Math.Max(1, _options.SyncIntervalMinutes));
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider.GetRequiredService<ISportsDataProvider>();

            var upcoming = await provider.GetUpcomingFixturesAsync(cancellationToken);
            var results = await provider.GetResultsAsync(cancellationToken);

            // Phase 1: log-only. Replace with EF Core upsert when DB is wired.
            _logger.LogInformation(
                "Sports data sync: {UpcomingCount} upcoming, {ResultsCount} finished fixtures.",
                upcoming.Count,
                results.Count);

            foreach (var match in upcoming.Take(3))
            {
                _logger.LogDebug(
                    "Upcoming: {Home} vs {Away} at {Kickoff} ({Stage})",
                    match.HomeTeam.Name,
                    match.AwayTeam.Name,
                    match.KickoffUtc,
                    match.Stage);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Sports data sync failed.");
        }
    }
}
