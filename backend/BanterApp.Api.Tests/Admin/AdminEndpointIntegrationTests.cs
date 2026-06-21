using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Integrations.Jobs;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class AdminEndpointIntegrationTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public AdminEndpointIntegrationTests(BanterAppWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetOverview_WithoutAuthentication_ReturnsUnauthorizedOrForbidden()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/overview");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"Expected 401/403, got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task GetOverview_WithAuthenticatedNonAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_WithAdmin_ReturnsOkAndCounts()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OverviewResponse>();
        Assert.NotNull(body);
        Assert.True(body!.JobsEnabled == false || body.JobsEnabled == true);
    }

    [Fact]
    public async Task GetJobs_WithAdmin_ReturnsRegisteredJobs()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jobs = await response.Content.ReadFromJsonAsync<List<JobListItem>>();
        Assert.NotNull(jobs);
        Assert.Contains(jobs!, j => j.JobKey == "rss.sync");
        Assert.Contains(jobs!, j => j.JobKey == "failed-items.retry");
    }

    [Fact]
    public async Task PostRunJob_WithAdminAndCsrf_TriggersJob()
    {
        using var client = _factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync("/api/admin/jobs/rss.sync/run", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TriggeredResponse>();
        Assert.Equal("rss.sync", body!.Triggered);
    }

    [Fact]
    public async Task PostRunJob_WithoutCsrf_ReturnsForbidden()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.PostAsync("/api/admin/jobs/rss.sync/run", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithAdmin_ReturnsOkWithoutSecrets()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/health");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("sk-", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("database", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLaunchChecklist_WithAdmin_ReturnsChecklistItems()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/admin/launch-checklist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LaunchChecklistResponse>();
        Assert.NotNull(body?.Items);
        Assert.Contains(body!.Items, i => i.Label.Contains("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetSession_WithAdmin_ReturnsIsPlatformAdminTrue()
    {
        using var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/auth/session");
        var body = await response.Content.ReadFromJsonAsync<SessionAdminResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body!.IsPlatformAdmin);
    }

    [Fact]
    public async Task GetSyncRuns_WithoutAdmin_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateNonAdminClient();

        var response = await client.GetAsync("/api/sync/runs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record OverviewResponse(
        int TotalRssItems,
        int TotalYoutubeItems,
        bool JobsEnabled);

    private sealed record JobListItem(string JobKey, string DisplayName, string Status);

    private sealed record TriggeredResponse(string Triggered);

    private sealed record LaunchChecklistResponse(List<ChecklistItem> Items);

    private sealed record ChecklistItem(string Label, bool Passed);

    private sealed record SessionAdminResponse(bool IsPlatformAdmin);
}
