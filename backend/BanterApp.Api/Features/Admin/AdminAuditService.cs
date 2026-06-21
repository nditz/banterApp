using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Admin;

public interface IAdminAuditService
{
    Task LogAsync(
        IUserContext user,
        HttpContext http,
        string action,
        string targetType,
        string? targetId = null,
        object? metadata = null,
        CancellationToken ct = default);
}

public sealed class AdminAuditService(AppDbContext db) : IAdminAuditService
{
    public async Task LogAsync(
        IUserContext user,
        HttpContext http,
        string action,
        string targetType,
        string? targetId = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
        {
            return;
        }

        var entry = new AdminAuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = user.UserId.Value,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null
                ? null
                : SecretSanitizer.SanitizeJson(JsonSerializer.Serialize(metadata)),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.AdminAuditLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }
}
