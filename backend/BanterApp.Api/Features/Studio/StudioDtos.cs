namespace BanterApp.Api.Features.Studio;

public sealed record StudioPickEntry(
    string Name,
    string Role,
    string? Organization,
    string Prediction,
    string PredictionType,
    int? PointsAwarded,
    string? Archetype = null,
    string? ParodyCue = null,
    string? StyleSlug = null,
    bool IsFictionalPersona = false,
    string? AttributionNote = null,
    string? SourceUrl = null,
    string? SourcePlatform = null,
    string? AvatarSeed = null);

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
