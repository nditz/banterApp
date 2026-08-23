using System.Text.Json;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Xunit;

namespace BanterApp.Api.Tests;

public class ApiFootballFixtureMapperTests
{
    [Fact]
    public void MapFixtures_ParsesSingleFixture()
    {
        const string json = """
            {
              "response": [
                {
                  "fixture": { "id": 42, "date": "2026-06-15T18:00:00+00:00", "status": { "short": "NS" }, "venue": { "name": "Test Stadium" } },
                  "league": { "id": 1, "round": "Group A - 1" },
                  "teams": {
                    "home": { "id": 10, "name": "England", "code": "ENG" },
                    "away": { "id": 11, "name": "Brazil", "code": "BRA" }
                  },
                  "goals": { "home": null, "away": null }
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var fixtures = ApiFootballFixtureMapper.MapFixtures(document.RootElement);

        Assert.Single(fixtures);
        var match = fixtures[0];
        Assert.Equal("apifb-42", match.Id);
        Assert.Equal("England", match.HomeTeam.Name);
        Assert.Equal("Brazil", match.AwayTeam.Name);
        Assert.Equal("A", match.Group);
    }

    [Fact]
    public void MapFixtures_ParsesPremierLeagueMatchweek()
    {
        const string json = """
            {
              "response": [
                {
                  "fixture": { "id": 9001, "date": "2026-08-15T14:00:00+00:00", "status": { "short": "NS" }, "venue": { "name": "Emirates Stadium" } },
                  "league": { "id": 39, "round": "Regular Season - 1" },
                  "teams": {
                    "home": { "id": 42, "name": "Arsenal", "code": "ARS", "logo": "https://media.api-sports.io/football/teams/42.png" },
                    "away": { "id": 49, "name": "Chelsea", "code": "CHE", "logo": "https://media.api-sports.io/football/teams/49.png" }
                  },
                  "goals": { "home": null, "away": null }
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var fixtures = ApiFootballFixtureMapper.MapFixtures(document.RootElement);

        Assert.Single(fixtures);
        var match = fixtures[0];
        Assert.Equal("apifb-9001", match.Id);
        Assert.Equal("Arsenal", match.HomeTeam.Name);
        Assert.Equal("PL", match.Group);
        Assert.Equal(1, match.MatchweekNumber);
        Assert.Equal("https://media.api-sports.io/football/teams/42.png", match.HomeTeam.LogoUrl);
    }

    [Fact]
    public void MapEvents_ParsesGoalEvent()
    {
        const string json = """
            {
              "response": [
                {
                  "id": 99,
                  "time": { "elapsed": 23 },
                  "type": "Goal",
                  "detail": "Normal Goal",
                  "player": { "name": "Kane" },
                  "team": { "id": 10, "name": "England", "code": "ENG" }
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var events = ApiFootballFixtureMapper.MapEvents(document.RootElement);

        Assert.Single(events);
        Assert.Equal("Goal", events[0].Type);
        Assert.Equal(23, events[0].Minute);
        Assert.Equal("Kane", events[0].PlayerName);
    }

    [Fact]
    public void MapStandings_GroupsByLetter()
    {
        const string json = """
            {
              "response": [
                {
                  "league": {
                    "standings": [
                      [
                        {
                          "rank": 1,
                          "group": "Group A",
                          "team": { "id": 1, "name": "England", "code": "ENG" },
                          "all": { "played": 1, "win": 1, "draw": 0, "lose": 0, "goals": { "for": 2, "against": 0 } },
                          "goalsDiff": 2,
                          "points": 3
                        }
                      ]
                    ]
                  }
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var standings = ApiFootballFixtureMapper.MapStandings(document.RootElement);

        Assert.True(standings.ContainsKey("A"));
        Assert.Single(standings["A"]);
        Assert.Equal("ENG", standings["A"][0].Team.Code);
        Assert.Equal(3, standings["A"][0].Points);
    }
}

public class MockSportsDataProviderTests
{
    [Fact]
    public async Task GetAllFixtures_ReturnsPremierLeagueFixtures()
    {
        var provider = new MockSportsDataProvider();
        var fixtures = await provider.GetAllFixturesAsync();
        Assert.True(fixtures.Count >= 12);
        Assert.All(fixtures, f => Assert.Equal("PL", f.Group));
    }

    [Fact]
    public async Task GetAllStandings_ReturnsPremierLeagueTable()
    {
        var provider = new MockSportsDataProvider();
        var standings = await provider.GetAllStandingsAsync();
        Assert.Contains(standings.Keys, k => k == "PL");
    }
}
