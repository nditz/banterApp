namespace BanterApp.Api.Services;

public sealed class ErrorTrackRequest
{
    public required string Source { get; init; }
    public required string ErrorCode { get; init; }
    public required string MessageSafe { get; init; }
    public string Severity { get; init; } = "error";
    public string? ErrorType { get; init; }
    public string? MessageInternal { get; init; }
    public string? StackTrace { get; init; }
    public string? RequestId { get; init; }
    public string? Route { get; init; }
    public string? Method { get; init; }
    public int? StatusCode { get; init; }
    public Guid? UserId { get; init; }
    public Guid? AdminUserId { get; init; }
    public string? JobKey { get; init; }
    public Guid? JobRunId { get; init; }
    public Guid? SourceItemId { get; init; }
    public string? Provider { get; init; }
    public string? ProviderRequestId { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
    public bool IsRetryable { get; init; }
    public int RetryCount { get; init; }
    public DateTimeOffset? NextRetryAt { get; init; }
    public bool SkipPersistence { get; init; }
}
