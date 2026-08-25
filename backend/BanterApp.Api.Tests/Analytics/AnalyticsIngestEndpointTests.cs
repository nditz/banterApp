using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Features.Analytics;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests.Analytics;

public class AnalyticsIngestEndpointTests
{
    [Fact]
    public async Task PostEvents_WithUnknownEventName_ReturnsBadRequest()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[]
            {
                new { eventName = "definitely_not_a_catalogued_event" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AnalyticsEvents.AnyAsync());
    }

    [Fact]
    public async Task PostEvents_WithoutConsent_AcceptsRequestButStoresNothing()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[]
            {
                new { eventName = "session_started" }
            }
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<IngestResponse>();
        Assert.Equal(0, body!.Accepted);
        Assert.Equal(1, body.Dropped);
        Assert.Equal("no_consent", body.Reason);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AnalyticsEvents.AnyAsync());
    }

    [Fact]
    public async Task PostEvents_WithConsent_StoresEventAndKeepsOnlyAllowlistedProperties()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);
        await GrantAnalyticsConsentAsync(client);

        var response = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[]
            {
                new
                {
                    eventName = "prediction_created",
                    properties = new Dictionary<string, object>
                    {
                        ["matchweek"] = 12,
                        ["predictionType"] = "score",
                        // Not in the catalog for this event, so it must not be stored.
                        ["recoveryKey"] = "super-secret-value"
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IngestResponse>();
        Assert.Equal(1, body!.Accepted);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.AnalyticsEvents.SingleAsync();
        Assert.Equal("prediction_created", stored.EventName);
        Assert.Equal("prediction", stored.Feature);
        Assert.Equal(TestUsers.AdminId, stored.UserId);
        Assert.Contains("matchweek", stored.PropertiesJson);
        Assert.DoesNotContain("recoveryKey", stored.PropertiesJson);
        Assert.DoesNotContain("super-secret-value", stored.PropertiesJson);
    }

    [Fact]
    public async Task PostEvents_WithOversizedBatch_ReturnsBadRequest()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var events = Enumerable
            .Range(0, AnalyticsEventCatalog.MaxBatchSize + 1)
            .Select(_ => new { eventName = "session_started" })
            .ToArray();

        var response = await client.PostAsJsonAsync("/api/analytics/events", new { events });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostEvents_WithoutCsrf_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[] { new { eventName = "session_started" } }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Catalog_ContainsNoFreeFormOrIdentifyingPropertyKeys()
    {
        var forbidden = new[] { "email", "token", "password", "prompt", "secret", "address", "text", "body" };

        foreach (var definition in AnalyticsEventCatalog.All)
        {
            foreach (var key in definition.AllowedProperties)
            {
                Assert.DoesNotContain(
                    forbidden,
                    f => key.Contains(f, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static async Task GrantAnalyticsConsentAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/consent", new
        {
            analytics = true,
            marketing = false
        });

        response.EnsureSuccessStatusCode();
    }

    private sealed record IngestResponse(int Accepted, int Dropped, string? Reason);
}
