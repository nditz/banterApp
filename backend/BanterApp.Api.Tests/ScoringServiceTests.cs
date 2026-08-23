using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class ScoringServiceTests
{
    [Fact]
    public void CalculatePerfectMatchweekBonus_AwardsWhenEveryResultHits()
    {
        var scoring = new ScoringService();
        var matches = new List<Match>
        {
            Finished("m1", 2, 1),
            Finished("m2", 0, 0),
        };
        var picks = new List<Prediction>
        {
            ResultPick("m1", "H"),
            ResultPick("m2", "D"),
        };

        Assert.Equal(ScoringService.PerfectMatchweekBonus, scoring.CalculatePerfectMatchweekBonus(picks, matches));
    }

    [Fact]
    public void CalculatePerfectMatchweekBonus_ZeroWhenAFixtureIsMissing()
    {
        var scoring = new ScoringService();
        var matches = new List<Match>
        {
            Finished("m1", 1, 0),
            Finished("m2", 1, 1),
        };
        var picks = new List<Prediction>
        {
            ResultPick("m1", "H"),
        };

        Assert.Equal(0, scoring.CalculatePerfectMatchweekBonus(picks, matches));
    }

    [Fact]
    public void CalculatePoints_RescoresFinishedMatchWithoutSavingAgain()
    {
        var scoring = new ScoringService();
        var match = Finished("m1", 3, 1);

        Assert.Equal(3, scoring.CalculatePoints(PredictionType.Result, "H", match));
        Assert.Equal(7, scoring.CalculatePoints(PredictionType.CorrectScore, "3-1", match));
        Assert.Equal(2, scoring.CalculatePoints(PredictionType.DoubleChance, "1X", match));
        Assert.Equal(0, scoring.CalculatePoints(PredictionType.Result, "A", match));
    }

    private static Match Finished(string id, int home, int away) => new()
    {
        Id = id,
        TeamA = "Home",
        TeamB = "Away",
        KickoffTime = DateTimeOffset.UtcNow.AddHours(-2),
        Status = "FT",
        HomeScore = home,
        AwayScore = away,
        MatchweekNumber = 1
    };

    private static Prediction ResultPick(string matchId, string value) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        PredictionType = PredictionType.Result,
        PredictionValue = value
    };
}
