namespace BanterApp.Api.Features.Matches;

public sealed record MatchResponse(
    string Id,
    string TeamA,
    string TeamB,
    string TeamACode,
    string TeamBCode,
    DateTimeOffset KickoffTime,
    string Stage,
    string Group,
    string Venue,
    string Status,
    int? HomeScore,
    int? AwayScore);
