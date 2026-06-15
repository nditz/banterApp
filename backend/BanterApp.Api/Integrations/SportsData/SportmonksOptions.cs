namespace BanterApp.Api.Integrations.SportsData;

public sealed class SportmonksOptions
{
    public const string SectionName = "Sportmonks";

    public string BaseUrl { get; set; } = "https://api.sportmonks.com/v3/football";

    public string? Token { get; set; }

    public int WorldCupLeagueId { get; set; } = 732;

    /// <summary>FIFA World Cup 2026 season on Sportmonks (fixtureSeasons filter).</summary>
    public int WorldCupSeasonId { get; set; } = 26618;
}
