namespace BanterApp.Api.Integrations.SportsData;

public sealed class FootballDataOptions
{
    public const string SectionName = "FootballData";

    public string BaseUrl { get; set; } = "https://api.football-data.org/v4";

    public string? Token { get; set; }

    public string CompetitionCode { get; set; } = "PL";
}
