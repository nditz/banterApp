using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public interface IAuthAuditService
{
    Task LogAsync(
        string eventType,
        bool success,
        string? email = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        CancellationToken ct = default);
}

public sealed class AuthAuditService(AppDbContext db) : IAuthAuditService
{
    public async Task LogAsync(
        string eventType,
        bool success,
        string? email = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null,
        CancellationToken ct = default)
    {
        db.AuthAuditLogs.Add(new AuthAuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Email = email,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            Details = details
        });
        await db.SaveChangesAsync(ct);
    }
}
