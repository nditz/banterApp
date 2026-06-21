namespace BanterApp.Api.Data.Entities;

public class AuthAuditLog
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
