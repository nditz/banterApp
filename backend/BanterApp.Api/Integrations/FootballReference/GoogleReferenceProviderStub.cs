using BanterApp.Api.Integrations.FootballReference.Dtos;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Integrations.FootballReference;

/// <summary>
/// Optional Google APIs enrichment stub — returns empty data until configured.
/// </summary>
public sealed class GoogleReferenceProviderStub(ILogger<GoogleReferenceProviderStub> logger)
    : IFootballReferenceDataProvider
{
    public string ProviderName => "googleapis";

    public bool IsConfigured => false;

    private void LogStub(string method)
    {
        logger.LogWarning(
            "Google reference provider is not implemented; {Method} returned empty.",
            method);
    }

    public Task<IReadOnlyList<CountryDto>> SyncCountriesAsync(CancellationToken cancellationToken = default)
    {
        LogStub(nameof(SyncCountriesAsync));
        return Task.FromResult<IReadOnlyList<CountryDto>>([]);
    }

    public Task<IReadOnlyList<PlayerDto>> SyncPlayersAsync(
        SyncPlayersParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogStub(nameof(SyncPlayersAsync));
        return Task.FromResult<IReadOnlyList<PlayerDto>>([]);
    }

    public Task<IReadOnlyList<PlayerStatsDto>> SyncPlayerStatsAsync(
        SyncStatsParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogStub(nameof(SyncPlayerStatsAsync));
        return Task.FromResult<IReadOnlyList<PlayerStatsDto>>([]);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopScorersAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogStub(nameof(SyncTopScorersAsync));
        return Task.FromResult<IReadOnlyList<LeaderboardEntryDto>>([]);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopAssistsAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        LogStub(nameof(SyncTopAssistsAsync));
        return Task.FromResult<IReadOnlyList<LeaderboardEntryDto>>([]);
    }
}
