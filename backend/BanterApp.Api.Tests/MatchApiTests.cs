using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task CurrentMatchweek_ReturnsOpenRound()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/matchweeks/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentMatchweekPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Number);
        Assert.Equal(10, payload.Matches.Count);
        Assert.Contains(
            payload.Matches,
            m => m.TeamA == "Liverpool" && m.TeamB == "Arsenal");
        Assert.Equal("ok", payload.Status);
        Assert.False(payload.Official);
    }

    [Fact]
    public async Task Upcoming_ReturnsPremierLeagueFixturesAndIgnoresWorldCupRows()
    {
        using var client = _factory.CreateClient();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Matches.Add(new Match
            {
                Id = "of26-ko-999",
                TeamA = "Mexico",
                TeamB = "South Africa",
                TeamACode = "MEX",
                TeamBCode = "RSA",
                KickoffTime = DateTimeOffset.UtcNow.AddDays(3),
                Stage = "Round of 32",
                Group = "A",
                Venue = "Mexico City",
                Status = "NS"
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/matches/upcoming");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await response.Content.ReadFromJsonAsync<List<MatchPayload>>(JsonOptions);
        Assert.NotNull(matches);
        Assert.DoesNotContain(matches, m => m.TeamA == "Mexico");
        Assert.True(matches.Count > 0);
        Assert.All(matches, m => Assert.NotEqual("Mexico", m.TeamA));
    }

    [Fact]
    public async Task Standings_RanksTwentyClubsWithBadges()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<StandingsApiPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.Equal(20, payload.Rows.Count);
        Assert.Equal("ARS", payload.Rows[0].TeamCode);
        Assert.Equal(2, payload.Rows.Single(r => r.TeamCode == "CHE").Played);
        Assert.All(payload.Rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.LogoUrl));
            Assert.DoesNotContain("flagcdn.com", row.LogoUrl, StringComparison.OrdinalIgnoreCase);
        });
    }

    private sealed record CurrentMatchweekPayload(
        int Number,
        List<MatchPayload> Matches,
        string? Status,
        bool Official);

    private sealed record MatchPayload(string TeamA, string TeamB, string? Status);

    private sealed record StandingPayload(string TeamCode, int Played, string? LogoUrl);

    private sealed record StandingsApiPayload(string Status, List<StandingPayload> Rows);
}
