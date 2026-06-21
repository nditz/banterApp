namespace BanterApp.Api.Data.Entities;

public class SyncRun
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Status { get; set; } = "running";
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsSkipped { get; set; }
    public string? MetadataJson { get; set; }
}
