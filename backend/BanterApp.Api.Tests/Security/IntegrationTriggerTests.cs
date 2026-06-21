using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class IntegrationTriggerTests : IClassFixture<BanterAppWebApplicationFactory>
{
    [Fact]
    public async Task PostIntegrationsYoutubeSync_IsNotFoundOrUnauthorized()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync("/api/integrations/youtube/sync", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostIntegrationsRssSync_IsNotFound()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync("/api/integrations/rss/sync", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
