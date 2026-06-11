namespace BanterApp.Api.Integrations.SportsData.Dtos;

public sealed record MatchStatisticsDto(
    string MatchId,
    int HomePossessionPercent,
    int AwayPossessionPercent,
    int HomeShots,
    int AwayShots,
    int HomeShotsOnTarget,
    int AwayShotsOnTarget,
    int HomeCorners,
    int AwayCorners,
    int HomeFouls,
    int AwayFouls,
    int HomeYellowCards,
    int AwayYellowCards,
    int HomeRedCards,
    int AwayRedCards);
