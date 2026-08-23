namespace BanterApp.Api.Integrations.FootballReference;

public sealed class FootballReferenceDataOptions
{
    public const string SectionName = "FootballReferenceData";

    public string Provider { get; set; } = "api_sports";

    public string CompetitionCode { get; set; } = "PL";

    public string Season { get; set; } = "2026";

    public DateTimeOffset? PredictionLockDeadline { get; set; }

    /// <summary>API-Football league ID for the active domestic competition.</summary>
    public int LeagueId { get; set; } = 39;
}
