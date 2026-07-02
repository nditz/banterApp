using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Features.Ai;

/// <summary>
/// Grounded match facts assembled for pundit script generation.
/// </summary>
public sealed record MatchScriptContext(
    MatchDto Match,
    MatchStatisticsDto? Statistics,
    IReadOnlyList<MatchEventDto> Events,
    IReadOnlyList<LineupPlayerDto> Lineups,
    IReadOnlyList<StandingDto> Standings);
