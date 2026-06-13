namespace BanterApp.Api.Integrations.SportsData;

public sealed class SportsDataOptions
{
    public const string SectionName = "SportsData";

    public string Provider { get; set; } = "mock";

    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://v3.football.api-sports.io";

    public int WorldCupLeagueId { get; set; } = 1;

    public int WorldCupSeason { get; set; } = 2026;

    /// <summary>How often the Hangfire score sync job runs (minutes).</summary>
    public int SyncIntervalMinutes { get; set; } = 5;
}
