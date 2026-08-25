namespace BanterApp.Api.Features.Analytics;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>
    /// How long raw events are kept before <c>analytics.retention.cleanup</c> removes
    /// them. Read in one place so the retention policy is never inlined into business
    /// logic.
    /// </summary>
    public int RawEventRetentionDays { get; set; } = 180;

    /// <summary>Rows deleted per statement during retention cleanup.</summary>
    public int RetentionDeleteBatchSize { get; set; } = 5000;
}
