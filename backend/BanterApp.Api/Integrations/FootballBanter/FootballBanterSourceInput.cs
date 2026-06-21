namespace BanterApp.Api.Integrations.FootballBanter;

public sealed class FootballBanterSourceInput
{
    public string SourceType { get; set; } = "article";

    public string SourceName { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string? SourceTitle { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? PunditName { get; set; }

    public string SourceText { get; set; } = string.Empty;

    public string? Prediction { get; set; }

    public double? Confidence { get; set; }

    public FootballBanterStatementType? StatementType { get; set; }
}
