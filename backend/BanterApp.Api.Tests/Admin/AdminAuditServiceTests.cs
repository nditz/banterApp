using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class AdminAuditServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsSanitizedAuditEntry()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new AdminAuditService(db);
        var userId = Guid.NewGuid();
        var user = new UserContext { UserId = userId };
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        http.Request.Headers.UserAgent = "xunit";

        await service.LogAsync(
            user,
            http,
            action: "job.run",
            targetType: "job",
            targetId: "rss.sync",
            metadata: new { api_key = "secret-value", job = "rss.sync" });

        var entry = await db.AdminAuditLogs.SingleAsync();
        Assert.Equal(userId, entry.AdminUserId);
        Assert.Equal("job.run", entry.Action);
        Assert.Contains("[REDACTED]", entry.MetadataJson);
        Assert.DoesNotContain("secret-value", entry.MetadataJson);
    }

    [Fact]
    public async Task LogAsync_UnauthenticatedUser_DoesNotPersist()
    {
        await using var db = TestDbContextFactory.Create();
        var service = new AdminAuditService(db);
        var user = new UserContext { AnonymousUserId = Guid.NewGuid() };

        await service.LogAsync(
            user,
            new DefaultHttpContext(),
            action: "job.run",
            targetType: "job");

        Assert.Empty(await db.AdminAuditLogs.ToListAsync());
    }
}
