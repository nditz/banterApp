using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public interface ISportsDataProvider
{
    Task<IReadOnlyList<MatchDto>> GetAllFixturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchDto>> GetResultsAsync(CancellationToken cancellationToken = default);

    /// <summary>In-play fixtures from the canonical sports provider (football-data.org or API-Football).</summary>
    Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(CancellationToken cancellationToken = default);

    Task<MatchStatisticsDto?> GetMatchStatisticsAsync(string matchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StandingDto>> GetStandingsAsync(string group, CancellationToken cancellationToken = default);
}
