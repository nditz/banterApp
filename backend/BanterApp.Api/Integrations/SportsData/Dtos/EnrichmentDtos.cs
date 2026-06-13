namespace BanterApp.Api.Integrations.SportsData.Dtos;

public sealed record MatchEventDto(
    string ProviderEventId,
    int Minute,
    string Type,
    string? TeamCode,
    string? PlayerName,
    string? Detail);

public sealed record LineupPlayerDto(
    string TeamCode,
    int? ShirtNumber,
    string PlayerName,
    string Position,
    bool IsSubstitute);

public sealed record SquadPlayerDto(
    string ProviderPlayerId,
    string Name,
    int? Number,
    string? Position);

public sealed record TeamSquadDto(
    string TeamId,
    string TeamName,
    string TeamCode,
    IReadOnlyList<SquadPlayerDto> Players);
