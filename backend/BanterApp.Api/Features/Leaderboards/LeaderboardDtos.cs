namespace BanterApp.Api.Features.Leaderboards;

public sealed record LeaderboardEntry(
    Guid? UserId,
    string DisplayName,
    int TotalPoints,
    int PredictionsCount,
    int Rank,
    bool IsCurrentUser = false,
    /// <summary>Places gained since last week. Positive is upward movement.</summary>
    int RankDelta = 0,
    int WeeklyPoints = 0);

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
