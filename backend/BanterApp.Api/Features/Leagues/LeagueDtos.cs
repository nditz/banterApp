namespace BanterApp.Api.Features.Leagues;

public sealed record CreateLeagueRequest(string Name);

public sealed record JoinLeagueRequest(string InviteCode);

public sealed record LeagueResponse(
    Guid Id,
    string Name,
    string InviteCode,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt);

public sealed record LeagueStandingEntry(
    Guid UserId,
    string DisplayName,
    int TotalPoints,
    int PredictionsCount);

public sealed record LeagueStandingsResponse(
    Guid LeagueId,
    string LeagueName,
    IReadOnlyList<LeagueStandingEntry> Standings);
