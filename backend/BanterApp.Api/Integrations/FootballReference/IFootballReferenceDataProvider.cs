using BanterApp.Api.Integrations.FootballReference.Dtos;

namespace BanterApp.Api.Integrations.FootballReference;

public interface IFootballReferenceDataProvider
{
    string ProviderName { get; }

    bool IsConfigured { get; }

    Task<IReadOnlyList<CountryDto>> SyncCountriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlayerDto>> SyncPlayersAsync(
        SyncPlayersParams? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlayerStatsDto>> SyncPlayerStatsAsync(
        SyncStatsParams? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopScorersAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopAssistsAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default);
}
