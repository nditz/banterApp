namespace BanterApp.Api.Integrations.SportsData;

public sealed class SportmonksOptions
{
    public const string SectionName = "Sportmonks";

    public string BaseUrl { get; set; } = "https://api.sportmonks.com/v3/football";

    public string? Token { get; set; }

    public int LeagueId { get; set; } = 0;

    /// <summary>Sportmonks season id for the active domestic competition.</summary>
    public int SeasonId { get; set; } = 0;
}
