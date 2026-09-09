using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public class HealthAndBotProtectionTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public HealthAndBotProtectionTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthStatus>();
        Assert.Equal("ok", body?.Status);
    }

    [Fact]
    public async Task ApiHealth_ReportsDatabase()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"database\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"connected\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CurlUserAgent_CanProbeHealthPaths()
    {
        using var client = CurlClient();

        var liveness = await client.GetAsync("/health");
        var readiness = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
    }

    [Fact]
    public async Task CurlUserAgent_IsBlockedOnAppRoutes()
    {
        using var client = CurlClient();
        var response = await client.GetAsync("/api/feed");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BanterApp.Api.Data.AppDbContext>();
        var botInternal = db.OperationalErrors.ToList()
            .Where(e => e.ErrorCode == "INTERNAL_SERVER_ERROR" && e.JobKey == "bot_blocked")
            .ToList();
        Assert.Empty(botInternal);
    }

    private HttpClient CurlClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "curl/8.5.0");
        return client;
    }

    private sealed record HealthStatus(string Status);
}
