namespace BanterApp.Api.Features.Matches;

public sealed record MatchResolutionResult(
    string? MatchId,
    string? TeamA,
    string? TeamB,
    double Confidence);

public sealed record MatchCatalogEntry(
    string Id,
    string TeamA,
    string TeamB,
    string TeamACode,
    string TeamBCode,
    DateTimeOffset KickoffTime,
    string Stage,
    string Group);
