using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class PremierLeagueMatchweekTests
{
    [Fact]
    public async Task MockFixtures_CoverOpeningWeeksAndACurrentSeptemberRound()
    {
        var provider = new MockSportsDataProvider();
        var fixtures = await provider.GetAllFixturesAsync();

        Assert.Equal(40, fixtures.Count);
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 1));
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 2));
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 3));
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 4));

        var opener = fixtures.Single(m => m.Id == "pl26-mw1-1");
        Assert.Equal("Arsenal", opener.HomeTeam.Name);
        Assert.Equal("Coventry City", opener.AwayTeam.Name);
        Assert.Equal(3, opener.HomeScore);
        Assert.Equal(0, opener.AwayScore);
        Assert.Equal("FT", opener.Status);

        var monday = fixtures.Single(m => m.Id == "pl26-mw1-10");
        Assert.Equal("Fulham", monday.HomeTeam.Name);
        Assert.Equal("Chelsea", monday.AwayTeam.Name);
        Assert.Equal("FT", monday.Status);
        Assert.Equal(0, monday.HomeScore);
        Assert.Equal(2, monday.AwayScore);
    }

    [Fact]
    public async Task MockStandings_IncludePlayedMatchesAfterTwoWeeks()
    {
        var provider = new MockSportsDataProvider();
        var table = await provider.GetStandingsAsync("PL");

        Assert.Equal(20, table.Count);
        Assert.All(table, row => Assert.Equal(2, row.Played));
        Assert.Equal("ARS", table[0].Team.Code);
        Assert.Equal(6, table[0].Points);
        Assert.Equal(4, table[0].GoalDifference);
    }

    [Fact]
    public async Task CurrentMatchweek_On9Sep2026_ResolvesToOpenWeek3()
    {
        var provider = new MockSportsDataProvider();
        var fixtures = await provider.GetAllFixturesAsync();
        var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        var week = CurrentMatchweek.Resolve(
            fixtures.Select(m => (m.MatchweekNumber, (string?)m.Status, (DateTimeOffset?)m.KickoffUtc)),
            now);
        Assert.Equal(3, week);
    }

    [Fact]
    public async Task CurrentMatchweek_WithoutKickoffs_StaysOnLowestUnfinishedRound()
    {
        var week = CurrentMatchweek.Resolve(
        [
            (1, "FT"),
            (2, "FT"),
            (3, "NS"),
            (3, "NS"),
        ]);
        Assert.Equal(3, week);
    }
}
