namespace BanterApp.Api.Data.Entities;

public class AppMetric
{
    public Guid Id { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public string? DimensionsJson { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
