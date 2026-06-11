namespace BanterApp.Api.Integrations.SportsData.Dtos;

public sealed record StandingDto(
    int Rank,
    TeamDto Team,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);
