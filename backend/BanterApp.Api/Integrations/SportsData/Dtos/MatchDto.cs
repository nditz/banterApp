namespace BanterApp.Api.Integrations.SportsData.Dtos;

public sealed record MatchDto(
    string Id,
    TeamDto HomeTeam,
    TeamDto AwayTeam,
    DateTimeOffset KickoffUtc,
    string Stage,
    string Group,
    string Venue,
    string Status,
    int? HomeScore,
    int? AwayScore,
    int? MatchweekNumber = null);
