using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class RateLimitIntegrationTests : IClassFixture<BanterAppWebApplicationFactory>
{
    [Fact]
    public async Task PublicFeed_AllowsNormalTraffic()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/feed?page=1&pageSize=5");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
