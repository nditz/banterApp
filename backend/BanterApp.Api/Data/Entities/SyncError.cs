namespace BanterApp.Api.Data.Entities;

public class SyncError
{
    public Guid Id { get; set; }
    public Guid? SyncRunId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }

    public SyncRun? SyncRun { get; set; }
}
