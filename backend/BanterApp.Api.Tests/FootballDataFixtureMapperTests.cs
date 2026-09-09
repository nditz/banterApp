using System.Text.Json;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.SportsData;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class FootballDataFixtureMapperTests
{
    [Fact]
    public void MapMatches_stamps_premier_league_group_matchweek_and_status()
    {
        const string json = """
            {
              "matches": [
                {
                  "id": 497410,
                  "utcDate": "2026-08-14T19:00:00Z",
                  "status": "FINISHED",
                  "matchday": 1,
                  "stage": "REGULAR_SEASON",
                  "group": null,
                  "venue": "Emirates Stadium",
                  "homeTeam": {
                    "id": 57,
                    "name": "Arsenal FC",
                    "tla": "ARS",
                    "crest": "https://crests.football-data.org/57.png"
                  },
                  "awayTeam": {
                    "id": 65,
                    "name": "Nottingham Forest FC",
                    "tla": "NFO",
                    "crest": "https://crests.football-data.org/65.png"
                  },
                  "score": { "fullTime": { "home": 3, "away": 0 } }
                },
                {
                  "id": 497420,
                  "utcDate": "2026-09-12T14:00:00Z",
                  "status": "TIMED",
                  "matchday": 4,
                  "stage": "REGULAR_SEASON",
                  "homeTeam": { "id": 64, "name": "Liverpool FC", "tla": "LIV" },
                  "awayTeam": { "id": 73, "name": "Tottenham Hotspur FC", "tla": "TOT" },
                  "score": { "fullTime": { "home": null, "away": null } }
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var fixtures = FootballDataFixtureMapper.MapMatches(document.RootElement);

        Assert.Equal(2, fixtures.Count);
        Assert.All(fixtures, dto => Assert.True(PremierLeagueMatchScope.IsPremierLeagueDto(dto)));

        var finished = fixtures[0];
        Assert.Equal("fd-497410", finished.Id);
        Assert.Equal("PL", finished.Group);
        Assert.Equal("FT", finished.Status);
        Assert.Equal(1, finished.MatchweekNumber);
        Assert.Equal("Regular Season - 1", finished.Stage);
        Assert.Equal(3, finished.HomeScore);
        Assert.Equal(0, finished.AwayScore);
        Assert.Equal("ARS", finished.HomeTeam.Code);

        var upcoming = fixtures[1];
        Assert.Equal("NS", upcoming.Status);
        Assert.Equal(4, upcoming.MatchweekNumber);
        Assert.Null(upcoming.HomeScore);
    }

    [Fact]
    public void MapStandings_uses_pl_when_group_is_null()
    {
        const string json = """
            {
              "standings": [
                {
                  "stage": "REGULAR_SEASON",
                  "type": "TOTAL",
                  "group": null,
                  "table": [
                    {
                      "position": 1,
                      "team": { "id": 57, "name": "Arsenal FC", "tla": "ARS" },
                      "playedGames": 4,
                      "won": 3,
                      "draw": 1,
                      "lost": 0,
                      "goalsFor": 8,
                      "goalsAgainst": 2,
                      "goalDifference": 6,
                      "points": 10
                    }
                  ]
                }
              ]
            }
            """;

        using var document = JsonDocument.Parse(json);
        var standings = FootballDataFixtureMapper.MapStandings(document.RootElement);

        Assert.True(standings.TryGetValue("PL", out var rows));
        var row = Assert.Single(rows);
        Assert.Equal(1, row.Rank);
        Assert.Equal("ARS", row.Team.Code);
        Assert.Equal(10, row.Points);
    }

    [Fact]
    public void MapStatus_maps_football_data_enums()
    {
        Assert.Equal("FT", FootballDataFixtureMapper.MapStatus("FINISHED"));
        Assert.Equal("NS", FootballDataFixtureMapper.MapStatus("TIMED"));
        Assert.Equal("LIVE", FootballDataFixtureMapper.MapStatus("IN_PLAY"));
        Assert.Equal("PST", FootballDataFixtureMapper.MapStatus("POSTPONED"));
    }
}
