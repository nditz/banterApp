namespace BanterApp.Api.Integrations.SportsData;

public sealed class SportmonksOptions
{
    public const string SectionName = "Sportmonks";

    public string BaseUrl { get; set; } = "https://api.sportmonks.com/v3/football";

    public string? Token { get; set; }

    public int WorldCupLeagueId { get; set; } = 0;
}
