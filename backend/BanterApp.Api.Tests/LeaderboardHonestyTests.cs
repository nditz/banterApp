using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests;

public class LeaderboardHonestyTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BanterAppWebApplicationFactory _factory;

    public LeaderboardHonestyTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GlobalLeaderboard_WhenEmpty_DoesNotInventPlayers()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/leaderboards/global");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LeaderboardPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Empty(payload.Top);
        Assert.Equal(0, payload.TotalPlayers);
        Assert.Null(payload.Me);
    }

    [Fact]
    public async Task FriendsLeaderboard_DoesNotInventPlayers()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/leaderboards/friends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LeaderboardPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Empty(payload.Top);
        Assert.Equal(0, payload.TotalPlayers);
    }

    [Fact]
    public async Task PunditLeaderboard_OmitsNamesWithNoPremierLeagueTake()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/leaderboards/pundits");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<PunditRow>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.DoesNotContain(payload, p => p.Name == "Aidan O'Brien");
        Assert.DoesNotContain(payload, p => p.Name == "Side-View Gary");
    }

    private sealed record LeaderboardPayload(
        List<Row> Top,
        Row? Me,
        int TotalPlayers);

    private sealed record Row(string? DisplayName, int Rank);

    private sealed record PunditRow(string Name, int TotalPredictions);
}
