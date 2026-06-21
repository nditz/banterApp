using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class AdminErrorEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public AdminErrorEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetErrors_NonAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync("/api/admin/errors");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetErrors_Admin_ReturnsOperationalErrors()
    {
        SeedError(_factory);

        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/errors?severity=error");
        response.EnsureSuccessStatusCode();

        var errors = await response.Content.ReadFromJsonAsync<List<AdminErrorListItem>>();
        Assert.NotNull(errors);
        Assert.Contains(errors!, e => e.ErrorCode == ErrorCodes.JobFailed);
    }

    [Fact]
    public async Task InvestigateError_UpdatesStatus()
    {
        var id = SeedError(_factory);

        using var client = _factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync($"/api/admin/errors/{id}/investigate", null);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.OperationalErrors.FindAsync(id);
        Assert.Equal("investigating", row!.Status);
    }

    private static Guid SeedError(BanterAppWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.OperationalErrors.Add(new OperationalError
        {
            Id = id,
            Fingerprint = Guid.NewGuid().ToString("N"),
            Source = "job",
            Environment = "Development",
            Severity = "error",
            Status = "open",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = "Test job failed",
            JobKey = "rss.sync",
            FirstSeenAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            OccurrenceCount = 1
        });
        db.SaveChanges();
        return id;
    }

    private sealed record AdminErrorListItem(string ErrorCode, string Message, string Status);
}
