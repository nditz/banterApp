using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests.Analytics;

public class ConsentEndpointTests
{
    [Fact]
    public async Task GetConsent_BeforeAnyChoice_ReportsNotRecordedAndNothingAllowed()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.GetAsync("/api/consent");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConsentStateResponse>();

        Assert.False(body!.Recorded);
        Assert.False(body.AnalyticsAllowed);
        Assert.False(body.MarketingAllowed);
        Assert.False(string.IsNullOrWhiteSpace(body.CurrentConsentVersion));
    }

    [Fact]
    public async Task PostConsent_WithoutCsrf_ReturnsForbidden()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/consent", new
        {
            analytics = true,
            marketing = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostConsent_PersistsChoiceAndIsReadBack()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var saved = await client.PostAsJsonAsync("/api/consent", new
        {
            analytics = true,
            marketing = false
        });

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var readBack = await client.GetAsync("/api/consent");
        var body = await readBack.Content.ReadFromJsonAsync<ConsentStateResponse>();

        Assert.True(body!.Recorded);
        Assert.True(body.AnalyticsAllowed);
        Assert.False(body.MarketingAllowed);
        Assert.True(body.IsCurrentVersion);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = await db.ConsentPreferences.SingleAsync();
        Assert.Equal(TestUsers.AdminId, record.UserId);
        Assert.True(record.AnalyticsAllowed);
        Assert.False(record.MarketingAllowed);
    }

    [Fact]
    public async Task PostConsent_Twice_UpdatesTheSameRecord()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        await client.PostAsJsonAsync("/api/consent", new { analytics = true, marketing = true });
        await client.PostAsJsonAsync("/api/consent", new { analytics = false, marketing = false });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var record = await db.ConsentPreferences.SingleAsync();
        Assert.False(record.AnalyticsAllowed);
        Assert.False(record.MarketingAllowed);
    }

    [Fact]
    public async Task PostConsent_ThenWithdrawal_StopsAnalyticsIngest()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        await client.PostAsJsonAsync("/api/consent", new { analytics = true, marketing = false });
        await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[] { new { eventName = "session_started" } }
        });

        await client.PostAsJsonAsync("/api/consent", new { analytics = false, marketing = false });
        await client.PostAsJsonAsync("/api/analytics/events", new
        {
            events = new[] { new { eventName = "landing_viewed" } }
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.AnalyticsEvents.ToListAsync();
        Assert.Equal("session_started", Assert.Single(stored).EventName);
    }

    private sealed record ConsentStateResponse(
        bool Recorded,
        string ConsentVersion,
        bool AnalyticsAllowed,
        bool MarketingAllowed,
        DateTimeOffset? UpdatedAt,
        bool IsCurrentVersion,
        string CurrentConsentVersion);
}
