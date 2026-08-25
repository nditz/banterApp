using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class AdminUsersEndpointTests
{
    [Fact]
    public async Task GetUsers_WithoutAuthentication_IsDenied()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetUsers_WithAuthenticatedNonAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserDetail_WithAuthenticatedNonAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync($"/api/admin/users/{TestUsers.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithAdmin_ReturnsPagedUsersWithoutSecrets()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/users?pageSize=10");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserListResponse>();
        Assert.NotNull(body);
        Assert.Contains(body!.Items, u => u.Id == TestUsers.AdminId);
        Assert.DoesNotContain("serviceRoleKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostRole_WithoutCsrf_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{TestUsers.UserId}/roles",
            new { role = "admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostRole_WithAdmin_GrantsRoleAndWritesAuditRow()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{TestUsers.UserId}/roles",
            new { role = "admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Users.AnyAsync(u => u.Id == TestUsers.UserId && u.IsPlatformAdmin));

        var audit = await db.AdminAuditLogs
            .FirstOrDefaultAsync(l =>
                l.Action == "user.role.grant" &&
                l.TargetId == TestUsers.UserId.ToString());

        Assert.NotNull(audit);
        Assert.Equal(TestUsers.AdminId, audit!.AdminUserId);
        Assert.Equal("user", audit.TargetType);
    }

    [Fact]
    public async Task PostRole_WithUnknownRole_ReturnsBadRequest()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{TestUsers.UserId}/roles",
            new { role = "superuser" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_OnSelf_ReturnsConflictAndWritesNoAuditRow()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.DeleteAsync($"/api/admin/users/{TestUsers.AdminId}/roles/admin");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Users.AnyAsync(u => u.Id == TestUsers.AdminId && u.IsPlatformAdmin));
        Assert.False(await db.AdminAuditLogs.AnyAsync(l => l.Action == "user.role.revoke"));
    }

    [Fact]
    public async Task PostStatus_OnSelf_ReturnsConflict()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{TestUsers.AdminId}/status",
            new { status = "Suspended" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostStatus_WithAdmin_UpdatesStatusAndWritesAuditRow()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/users/{TestUsers.UserId}/status",
            new { status = "Suspended" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Id == TestUsers.UserId);
        Assert.Equal("Suspended", user.AccountStatus.ToString());
        Assert.True(await db.AdminAuditLogs.AnyAsync(l => l.Action == "user.status.change"));
    }

    [Fact]
    public async Task GetAuditLogs_WithAdmin_SupportsActionFilterAndPagination()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        await client.PostAsync("/api/admin/jobs/rss.sync/run", content: null);

        var filtered = await client.GetAsync("/api/admin/audit-logs?action=job.run&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);

        var body = await filtered.Content.ReadFromJsonAsync<AuditLogResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.PageSize);
        Assert.All(body.Items, i => Assert.Equal("job.run", i.Action));
        Assert.Contains("job.run", body.AvailableActions);

        var other = await client.GetAsync("/api/admin/audit-logs?action=user.role.grant");
        var otherBody = await other.Content.ReadFromJsonAsync<AuditLogResponse>();
        Assert.Empty(otherBody!.Items);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvertedDateRange_ReturnsBadRequest()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.GetAsync(
            "/api/admin/audit-logs?from=2026-08-02T00:00:00Z&to=2026-08-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithAuthenticatedNonAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync("/api/admin/audit-logs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record UserListResponse(
        List<UserListItem> Items,
        int Page,
        int PageSize,
        int Total,
        string IdentitySource);

    private sealed record UserListItem(Guid Id, string Email, bool IsPlatformAdmin);

    private sealed record AuditLogResponse(
        List<AuditLogItem> Items,
        int Page,
        int PageSize,
        int Total,
        List<string> AvailableActions);

    private sealed record AuditLogItem(Guid Id, string Action, string? TargetType);
}
