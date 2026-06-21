namespace BanterApp.Api.Data.Entities;

public class ProviderUsageDaily
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateOnly UsageDate { get; set; }
    public int RequestCount { get; set; }
    public int FailureCount { get; set; }
    public int EstimatedUnits { get; set; }
    public long TotalLatencyMs { get; set; }
    public int LatencySamples { get; set; }
    public double? AverageLatencyMs { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
