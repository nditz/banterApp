using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public interface ISportsDataProvider
{
    Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchDto>> GetResultsAsync(CancellationToken cancellationToken = default);

    Task<MatchStatisticsDto?> GetMatchStatisticsAsync(string matchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StandingDto>> GetStandingsAsync(string group, CancellationToken cancellationToken = default);
}
