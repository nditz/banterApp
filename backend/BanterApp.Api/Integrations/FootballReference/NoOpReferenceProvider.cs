using BanterApp.Api.Integrations.FootballReference.Dtos;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Integrations.FootballReference;

public sealed class NoOpReferenceProvider(ILogger<NoOpReferenceProvider> logger) : IFootballReferenceDataProvider
{
    public string ProviderName => "none";

    public bool IsConfigured => false;

    private void LogSkip()
    {
        logger.LogWarning("Football reference data provider is not configured; skipping sync.");
    }

    public Task<IReadOnlyList<CountryDto>> SyncCountriesAsync(CancellationToken cancellationToken = default)
    {
        LogSkip();
        return Task.FromResult<IReadOnlyList<CountryDto>>([]);
    }

    public Task<IReadOnlyList<PlayerDto>> SyncPlayersAsync(
        SyncPlayersParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogSkip();
        return Task.FromResult<IReadOnlyList<PlayerDto>>([]);
    }

    public Task<IReadOnlyList<PlayerStatsDto>> SyncPlayerStatsAsync(
        SyncStatsParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogSkip();
        return Task.FromResult<IReadOnlyList<PlayerStatsDto>>([]);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopScorersAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogSkip();
        return Task.FromResult<IReadOnlyList<LeaderboardEntryDto>>([]);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopAssistsAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogSkip();
        return Task.FromResult<IReadOnlyList<LeaderboardEntryDto>>([]);
    }
}
