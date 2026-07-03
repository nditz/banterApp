using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.FootballReference.Dtos;
using Hangfire;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.FootballReference.Jobs;

public sealed class FootballCountriesSyncJob
{
    public const string JobId = "football-countries-sync";

    private readonly FootballReferenceDataProviderFactory _factory;
    private readonly ReferenceDataUpsertService _upsert;
    private readonly SyncRunTracker _tracker;

    public FootballCountriesSyncJob(
        FootballReferenceDataProviderFactory factory,
        ReferenceDataUpsertService upsert,
        SyncRunTracker tracker)
    {
        _factory = factory;
        _upsert = upsert;
        _tracker = tracker;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            var items = await provider.SyncCountriesAsync(cancellationToken);
            var (created, updated) = await _upsert.UpsertCountriesAsync(items, provider.ProviderName, cancellationToken);
            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}

public sealed class FootballPlayersSyncJob
{
    public const string JobId = "football-players-sync";

    private readonly FootballReferenceDataProviderFactory _factory;
    private readonly ReferenceDataUpsertService _upsert;
    private readonly SyncRunTracker _tracker;
    private readonly FootballReferenceDataOptions _options;

    public FootballPlayersSyncJob(
        FootballReferenceDataProviderFactory factory,
        ReferenceDataUpsertService upsert,
        SyncRunTracker tracker,
        IOptions<FootballReferenceDataOptions> options)
    {
        _factory = factory;
        _upsert = upsert;
        _tracker = tracker;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            var items = await provider.SyncPlayersAsync(
                new SyncPlayersParams(_options.CompetitionCode, _options.Season, _options.LeagueId),
                cancellationToken);
            var (created, updated) = await _upsert.UpsertPlayersAsync(items, provider.ProviderName, cancellationToken);
            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}

public sealed class FootballPlayerStatsSyncJob
{
    public const string JobId = "football-player-stats-sync";

    private readonly FootballReferenceDataProviderFactory _factory;
    private readonly ReferenceDataUpsertService _upsert;
    private readonly SyncRunTracker _tracker;
    private readonly FootballReferenceDataOptions _options;

    public FootballPlayerStatsSyncJob(
        FootballReferenceDataProviderFactory factory,
        ReferenceDataUpsertService upsert,
        SyncRunTracker tracker,
        IOptions<FootballReferenceDataOptions> options)
    {
        _factory = factory;
        _upsert = upsert;
        _tracker = tracker;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            var items = await provider.SyncPlayerStatsAsync(
                new SyncStatsParams(_options.CompetitionCode, _options.Season, _options.LeagueId),
                cancellationToken);
            var (created, updated) = await _upsert.UpsertPlayerStatsAsync(items, provider.ProviderName, cancellationToken);
            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}

public sealed class FootballTopScorersSyncJob
{
    public const string JobId = "football-top-scorers-sync";

    private readonly FootballReferenceDataProviderFactory _factory;
    private readonly ReferenceDataUpsertService _upsert;
    private readonly SyncRunTracker _tracker;
    private readonly FootballReferenceDataOptions _options;

    public FootballTopScorersSyncJob(
        FootballReferenceDataProviderFactory factory,
        ReferenceDataUpsertService upsert,
        SyncRunTracker tracker,
        IOptions<FootballReferenceDataOptions> options)
    {
        _factory = factory;
        _upsert = upsert;
        _tracker = tracker;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            var items = await provider.SyncTopScorersAsync(
                new LeaderboardParams(_options.CompetitionCode, _options.Season, _options.LeagueId),
                cancellationToken);
            var (created, updated) = await _upsert.UpsertLeaderboardAsync(
                items,
                LeaderboardTypes.TopScorers,
                provider.ProviderName,
                _options.CompetitionCode,
                _options.Season,
                cancellationToken);
            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}

public sealed class FootballTopAssistsSyncJob
{
    public const string JobId = "football-top-assists-sync";

    private readonly FootballReferenceDataProviderFactory _factory;
    private readonly ReferenceDataUpsertService _upsert;
    private readonly SyncRunTracker _tracker;
    private readonly FootballReferenceDataOptions _options;

    public FootballTopAssistsSyncJob(
        FootballReferenceDataProviderFactory factory,
        ReferenceDataUpsertService upsert,
        SyncRunTracker tracker,
        IOptions<FootballReferenceDataOptions> options)
    {
        _factory = factory;
        _upsert = upsert;
        _tracker = tracker;
        _options = options.Value;
    }

    [AutomaticRetry(Attempts = 2, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            var items = await provider.SyncTopAssistsAsync(
                new LeaderboardParams(_options.CompetitionCode, _options.Season, _options.LeagueId),
                cancellationToken);
            var (created, updated) = await _upsert.UpsertLeaderboardAsync(
                items,
                LeaderboardTypes.TopAssists,
                provider.ProviderName,
                _options.CompetitionCode,
                _options.Season,
                cancellationToken);
            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}

public sealed class FootballReferenceFullSyncJob
{
    public const string JobId = "football-reference-full-sync";

    private readonly FootballCountriesSyncJob _countries;
    private readonly FootballPlayersSyncJob _players;
    private readonly FootballPlayerStatsSyncJob _stats;
    private readonly FootballTopScorersSyncJob _scorers;
    private readonly FootballTopAssistsSyncJob _assists;
    private readonly SyncRunTracker _tracker;
    private readonly FootballReferenceDataProviderFactory _factory;

    public FootballReferenceFullSyncJob(
        FootballCountriesSyncJob countries,
        FootballPlayersSyncJob players,
        FootballPlayerStatsSyncJob stats,
        FootballTopScorersSyncJob scorers,
        FootballTopAssistsSyncJob assists,
        SyncRunTracker tracker,
        FootballReferenceDataProviderFactory factory)
    {
        _countries = countries;
        _players = players;
        _stats = stats;
        _scorers = scorers;
        _assists = assists;
        _tracker = tracker;
        _factory = factory;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var provider = _factory.Resolve();
        var run = await _tracker.StartAsync(provider.ProviderName, JobId, cancellationToken);

        try
        {
            await _countries.SyncAsync(cancellationToken);
            await _players.SyncAsync(cancellationToken);
            await _stats.SyncAsync(cancellationToken);
            await _scorers.SyncAsync(cancellationToken);
            await _assists.SyncAsync(cancellationToken);
            await _tracker.CompleteAsync(run, 0, 0, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            await _tracker.FailAsync(run, 0, 0, ex, cancellationToken);
            throw;
        }
    }
}
