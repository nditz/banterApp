namespace BanterApp.Api.Integrations.FootballReference.Dtos;

public sealed record CountryDto(
    string ExternalId,
    string Name,
    string? Code,
    string? FlagUrl,
    string? Continent,
    int? FifaRanking,
    string? MetadataJson);

public sealed record PlayerDto(
    string ExternalId,
    string? CountryExternalId,
    string? FirstName,
    string? LastName,
    string DisplayName,
    string? KnownName,
    DateOnly? DateOfBirth,
    int? Age,
    string? Position,
    string? PhotoUrl,
    string? ClubName,
    string? NationalTeamName,
    string? MetadataJson);

public sealed record PlayerStatsDto(
    string PlayerExternalId,
    string? CountryExternalId,
    string? Competition,
    string? Season,
    int MatchesPlayed,
    int Goals,
    int Assists,
    int YellowCards,
    int RedCards,
    int MinutesPlayed,
    decimal? Rating,
    string? MetadataJson);

public sealed record LeaderboardEntryDto(
    string PlayerExternalId,
    string? CountryExternalId,
    int? Rank,
    decimal Value,
    string? MetadataJson);

public sealed record SyncPlayersParams(string? Competition, string? Season, int? LeagueId);

public sealed record SyncStatsParams(string? Competition, string? Season, int? LeagueId);

public sealed record LeaderboardParams(string? Competition, string? Season, int? LeagueId);
