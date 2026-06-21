using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ClientErrorEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public ClientErrorEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostClientError_StoresSanitizedFrontendError()
    {
        using var client = _factory.CreateClient();
        var payload = new
        {
            message = "Render failed",
            stack = "Error at api_key=super-secret",
            route = "/feed",
            component = "FeedList"
        };

        var response = await client.PostAsJsonAsync("/api/errors/client", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Request-Id"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = db.OperationalErrors.Single(e => e.MessageSafe == "Render failed");

        Assert.Equal("frontend", row.Source);
        Assert.DoesNotContain("super-secret", row.StackTrace ?? string.Empty);
    }

    [Fact]
    public async Task PostClientError_RejectsOversizedPayload()
    {
        using var client = _factory.CreateClient();
        var payload = new { message = new string('x', 3000) };

        var response = await client.PostAsJsonAsync("/api/errors/client", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
