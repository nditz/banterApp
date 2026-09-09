using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

/// <summary>
/// Isolated factory so replacing the seeded calendar does not leak into other match API tests.
/// </summary>
public class MatchReadPathTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BanterAppWebApplicationFactory _factory;

    public MatchReadPathTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Upcoming_WhenStoredCalendarIsOverdue_DoesNotSubstituteProviderFixtures()
    {
        await ReplacePremierLeagueCalendarWithOverdueAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/matches/upcoming");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var upcoming = await response.Content.ReadFromJsonAsync<List<MatchPayload>>(JsonOptions);
        Assert.NotNull(upcoming);
        Assert.Empty(upcoming);
        Assert.DoesNotContain(upcoming, m => m.TeamA == "Liverpool" && m.TeamB == "Arsenal");
    }

    [Fact]
    public async Task CurrentMatchweek_WhenStoredCalendarIsOverdue_IsStaleNotProviderWeek3()
    {
        await ReplacePremierLeagueCalendarWithOverdueAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/matchweeks/current");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentMatchweekPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Number);
        Assert.Equal("stale", payload.Status);
        Assert.False(payload.Official);
        Assert.All(payload.Matches, m => Assert.StartsWith("overdue-", m.Id, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ReplacePremierLeagueCalendarWithOverdueAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Matches.RemoveRange(db.Matches);
        await db.SaveChangesAsync();
        db.Matches.AddRange(
            Overdue("overdue-mw2-1", "Crystal Palace", "Manchester City", "CRY", "MCI"),
            Overdue("overdue-mw2-2", "Liverpool", "Nottingham Forest", "LIV", "NFO"));
        await db.SaveChangesAsync();
    }

    private static Match Overdue(string id, string home, string away, string homeCode, string awayCode) =>
        new()
        {
            Id = id,
            TeamA = home,
            TeamB = away,
            TeamACode = homeCode,
            TeamBCode = awayCode,
            KickoffTime = new DateTimeOffset(2026, 8, 28, 19, 0, 0, TimeSpan.Zero),
            Status = "NS",
            Stage = "Regular Season - 2",
            Group = "PL",
            Venue = "Test",
            MatchweekNumber = 2
        };

    private sealed record CurrentMatchweekPayload(
        int Number,
        List<IdMatchPayload> Matches,
        string? Status,
        bool Official);

    private sealed record IdMatchPayload(string Id, string TeamA, string TeamB);

    private sealed record MatchPayload(string TeamA, string TeamB);
}
