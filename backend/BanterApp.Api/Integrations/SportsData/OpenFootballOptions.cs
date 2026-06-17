namespace BanterApp.Api.Integrations.SportsData;

public sealed class OpenFootballOptions
{
    public const string SectionName = "OpenFootball";

    public bool Enabled { get; set; } = true;

    public string JsonUrl { get; set; } =
        "https://raw.githubusercontent.com/openfootball/worldcup.json/master/2026/worldcup.json";
}
