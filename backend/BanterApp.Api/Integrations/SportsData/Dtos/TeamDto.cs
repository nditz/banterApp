namespace BanterApp.Api.Integrations.SportsData.Dtos;

public sealed record TeamDto(
    string Id,
    string Name,
    string Code,
    string? ShortName = null,
    string? LogoUrl = null);
