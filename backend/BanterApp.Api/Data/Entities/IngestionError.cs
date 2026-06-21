namespace BanterApp.Api.Data.Entities;

public class IngestionError
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string JobKey { get; set; } = string.Empty;
    public string Severity { get; set; } = "error";
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? MetadataJson { get; set; }
    public string Status { get; set; } = "open";
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public int Count { get; set; } = 1;
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? SyncRunId { get; set; }
    public Guid? MediaItemId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
