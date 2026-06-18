namespace BanterApp.Api.Features.Leaderboards;

public sealed record LeaderboardEntry(
    Guid? UserId,
    string DisplayName,
    int TotalPoints,
    int PredictionsCount,
    int Rank,
    bool IsCurrentUser = false);

public sealed record LeaderboardView(
    IReadOnlyList<LeaderboardEntry> Top,
    LeaderboardEntry? Me,
    int TotalPlayers);

public sealed record PunditLeaderboardEntry(
    Guid PunditId,
    string Name,
    string Organization,
    string? Archetype,
    string? ParodyCue,
    string? StyleSlug,
    bool IsFictionalPersona,
    string? AttributionNote,
    string? AvatarSeed,
    string? SourceUrl,
    int CorrectPredictions,
    int TotalPredictions,
    int Rank);
