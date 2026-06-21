namespace BanterApp.Api.Data.Entities;

public class OperationalError
{
    public Guid Id { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Severity { get; set; } = "error";
    public string Status { get; set; } = "open";
    public string ErrorCode { get; set; } = string.Empty;
    public string? ErrorType { get; set; }
    public string MessageSafe { get; set; } = string.Empty;
    public string? MessageInternal { get; set; }
    public string? StackTrace { get; set; }
    public string? Route { get; set; }
    public string? Method { get; set; }
    public int? StatusCode { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AdminUserId { get; set; }
    public string? JobKey { get; set; }
    public Guid? JobRunId { get; set; }
    public Guid? SourceItemId { get; set; }
    public string? Provider { get; set; }
    public string? ProviderRequestId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
