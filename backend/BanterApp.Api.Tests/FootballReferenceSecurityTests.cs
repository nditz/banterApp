using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballReferenceSecurityTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public FootballReferenceSecurityTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicFootballEndpoints_DoNotExposeApiKeys()
    {
        using var client = _factory.CreateClient();

        var endpoints = new[]
        {
            "/api/football/countries",
            "/api/football/players",
            "/api/football/leaderboards/top-scorers",
            "/api/predictions/aggregates"
        };

        foreach (var path in endpoints)
        {
            var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("API_SPORTS_KEY", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api_key", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api_token", body, StringComparison.OrdinalIgnoreCase);
        }
    }
}
