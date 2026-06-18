namespace BanterApp.Api.Features.Leagues;

public sealed record CreateLeagueRequest(string Name);

public sealed record JoinLeagueRequest(string InviteCode);

public sealed record LeagueResponse(
    Guid Id,
    string Name,
    string InviteCode,
    int MemberCount,
    int MaxMembers,
    DateTimeOffset CreatedAt,
    string? MyDisplayName = null);

public sealed record MyLeagueResponse(
    Guid Id,
    string Name,
    string InviteCode,
    int MemberCount,
    int MaxMembers,
    bool IsAdmin,
    string MyDisplayName,
    int MyPoints,
    DateTimeOffset CreatedAt,
    string Kind,
    string? CountryCode = null,
    bool BonusPointsEnabled = false);

public sealed record LeagueLimitsResponse(
    int CustomLeaguesUsed,
    int CustomLeaguesMax,
    int TotalLeaguesUsed,
    int TotalLeaguesMax);

public sealed record MyLeaguesResponse(
    IReadOnlyList<MyLeagueResponse> Leagues,
    LeagueLimitsResponse Limits);

public sealed record LeaguePreviewResponse(
    Guid Id,
    string Name,
    string InviteCode,
    int MemberCount,
    int MaxMembers,
    bool IsFull);

public sealed record LeagueStandingEntry(
    Guid? UserId,
    string DisplayName,
    int TotalPoints,
    int PredictionsCount,
    int BonusPoints = 0);

public sealed record LeagueStandingsResponse(
    Guid LeagueId,
    string LeagueName,
    IReadOnlyList<LeagueStandingEntry> Standings,
    bool BonusPointsEnabled = false);
