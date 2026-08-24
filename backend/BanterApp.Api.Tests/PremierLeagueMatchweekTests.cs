using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class PremierLeagueMatchweekTests
{
    [Fact]
    public async Task MockFixtures_MatchOfficial2026_27Matchweeks()
    {
        var provider = new MockSportsDataProvider();
        var fixtures = await provider.GetAllFixturesAsync();

        Assert.Equal(20, fixtures.Count);
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 1));
        Assert.Equal(10, fixtures.Count(m => m.MatchweekNumber == 2));

        var opener = fixtures.Single(m => m.Id == "pl26-mw1-1");
        Assert.Equal("Arsenal", opener.HomeTeam.Name);
        Assert.Equal("Coventry City", opener.AwayTeam.Name);
        Assert.Equal(3, opener.HomeScore);
        Assert.Equal(0, opener.AwayScore);
        Assert.Equal("FT", opener.Status);

        var monday = fixtures.Single(m => m.Id == "pl26-mw1-10");
        Assert.Equal("Fulham", monday.HomeTeam.Name);
        Assert.Equal("Chelsea", monday.AwayTeam.Name);
        Assert.Equal("NS", monday.Status);
    }

    [Fact]
    public async Task MockStandings_MatchPremierLeagueTieBreakersAfterMatchweek1()
    {
        var provider = new MockSportsDataProvider();
        var table = await provider.GetStandingsAsync("PL");

        Assert.Equal(20, table.Count);
        Assert.Equal(
            ["BHA", "ARS", "BRE", "EVE", "HUL", "IPS", "MCI", "LEE", "LIV", "NEW", "CHE", "FUL"],
            table.Take(12).Select(r => r.Team.Code).ToArray());
        Assert.Equal(4, table[0].GoalDifference);
        Assert.Equal(0, table.Single(r => r.Team.Code == "CHE").Played);
        Assert.Equal(0, table.Single(r => r.Team.Code == "FUL").Played);
    }

    [Fact]
    public async Task CurrentMatchweek_StaysOnRoundUntilEveryFixtureIsFinished()
    {
        var provider = new MockSportsDataProvider();
        var fixtures = await provider.GetAllFixturesAsync();
        var week = CurrentMatchweek.Resolve(fixtures.Select(m => (m.MatchweekNumber, (string?)m.Status)));
        Assert.Equal(1, week);
    }
}
