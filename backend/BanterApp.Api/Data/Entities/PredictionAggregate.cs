namespace BanterApp.Api.Data.Entities;

public class PredictionAggregate
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string PredictionType { get; set; } = string.Empty;
    public string? ConsensusSummary { get; set; }
    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }
    public int NeutralCount { get; set; }
    public int SourceCount { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
