using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests;

public class MatchApiTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BanterAppWebApplicationFactory _factory;

    public MatchApiTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CurrentMatchweek_ReturnsOpenOfficialRound()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/matchweeks/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentMatchweekPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Number);
        Assert.Equal(10, payload.Matches.Count);
        Assert.Contains(
            payload.Matches,
            m => m.TeamA == "Arsenal" && m.TeamB == "Coventry City" && m.Status == "FT");
        Assert.Contains(
            payload.Matches,
            m => m.TeamA == "Fulham" && m.TeamB == "Chelsea" && m.Status == "NS");
    }

    [Fact]
    public async Task Standings_RanksTwentyClubsWithBadges()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var table = await response.Content.ReadFromJsonAsync<List<StandingPayload>>(JsonOptions);
        Assert.NotNull(table);
        Assert.Equal(20, table.Count);
        Assert.Equal("BHA", table[0].TeamCode);
        Assert.Equal(0, table.Single(r => r.TeamCode == "CHE").Played);
        Assert.All(table, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.LogoUrl));
            Assert.DoesNotContain("flagcdn.com", row.LogoUrl, StringComparison.OrdinalIgnoreCase);
        });
    }

    private sealed record CurrentMatchweekPayload(int Number, List<MatchPayload> Matches);

    private sealed record MatchPayload(string TeamA, string TeamB, string? Status);

    private sealed record StandingPayload(string TeamCode, int Played, string? LogoUrl);
}
