namespace BanterApp.Api.Features.Matches;

public sealed record MatchResponse(
    string Id,
    string TeamA,
    string TeamB,
    string TeamACode,
    string TeamBCode,
    string? HomeLogoUrl,
    string? AwayLogoUrl,
    DateTimeOffset KickoffTime,
    string Stage,
    string Group,
    int? MatchweekNumber,
    string Venue,
    string Status,
    int? HomeScore,
    int? AwayScore,
    bool IsLocked);

public sealed record MatchweekResponse(
    int Number,
    string Name,
    string Status,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int FixtureCount,
    int PredictedCount,
    bool IsCurrent);

public sealed record StandingRowResponse(
    int Rank,
    string TeamCode,
    string TeamName,
    string? LogoUrl,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDiff,
    int Points);
