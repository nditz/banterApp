using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Extended sports data endpoints beyond core fixtures (teams, squads, events, lineups).
/// Implemented by <see cref="ApiFootballProvider"/>; optional for mock/fallback providers.
/// </summary>
public interface ISportsDataEnrichment
{
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default);

    Task<TeamSquadDto?> GetTeamSquadAsync(string teamProviderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetAllStandingsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchEventDto>> GetMatchEventsAsync(
        string matchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LineupPlayerDto>> GetMatchLineupsAsync(
        string matchId,
        CancellationToken cancellationToken = default);
}
