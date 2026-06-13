namespace BanterApp.Api.Features.Studio;

public sealed record StudioPickEntry(
    string Name,
    string Role,          // "me" | "league" | "pundit"
    string? Organization, // pundit outlet
    string Prediction,
    string PredictionType,
    int? PointsAwarded);

public sealed record StudioMatchComparison(
    string MatchId,
    string TeamA,
    string TeamB,
    DateTimeOffset KickoffTime,
    string? Status,
    string? ActualResult,
    IReadOnlyList<StudioPickEntry> Picks);

public sealed record StudioComparisonResponse(
    IReadOnlyList<StudioMatchComparison> Matches,
    int MyTotalPoints,
    int? MyLeagueRank,
    int? LeagueTotal);
