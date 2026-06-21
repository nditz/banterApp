using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class CsrfProtectionTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public CsrfProtectionTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PostPrediction_WithoutCsrf_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Anonymous-Id", Guid.NewGuid().ToString("N"));

        var response = await client.PostAsJsonAsync("/api/predictions/create", new
        {
            matchId = "match-1",
            predictionType = "result",
            predictionValue = "home",
            turnstileToken = "dev-bypass"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
