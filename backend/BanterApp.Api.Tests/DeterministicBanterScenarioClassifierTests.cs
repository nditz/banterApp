using BanterApp.Api.Integrations.Banter;
using Xunit;

namespace BanterApp.Api.Tests;

public class DeterministicBanterScenarioClassifierTests
{
    [Fact]
    public void Classify_CorrectWinPrediction_ReturnsSmugOrLucky()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.HomeWin,
            home: 2,
            away: 1,
            correct: true));

        Assert.True(
            scenario is BanterScenario.SmugWinner or BanterScenario.LuckyEscape,
            scenario.ToString());
    }

    [Fact]
    public void Classify_IncorrectWinPrediction_ReturnsAgedBadly()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.AwayWin,
            home: 0,
            away: 2,
            correct: false));

        Assert.Equal(BanterScenario.PredictionAgedBadly, scenario);
    }

    [Fact]
    public void Classify_CorrectDraw_ReturnsNailedIt()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.Draw,
            MatchOutcomeKind.Draw,
            home: 1,
            away: 1,
            correct: true));

        Assert.Equal(BanterScenario.PredictionNailedIt, scenario);
    }

    [Fact]
    public void Classify_IncorrectDrawPrediction_ReturnsUnderdogUpset()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.Draw,
            MatchOutcomeKind.HomeWin,
            home: 2,
            away: 1,
            correct: false));

        Assert.Equal(BanterScenario.UnderdogUpset, scenario);
    }

    [Fact]
    public void Classify_ActualDrawWhenIncorrect_ReturnsDrawFrustration()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.Draw,
            home: 1,
            away: 1,
            correct: false));

        Assert.Equal(BanterScenario.DrawFrustration, scenario);
    }

    [Fact]
    public void Classify_HeavyLossWhenIncorrect_ReturnsAgedBadly()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.AwayWin,
            home: 0,
            away: 4,
            correct: false));

        Assert.Equal(BanterScenario.PredictionAgedBadly, scenario);
    }

    [Fact]
    public void Classify_DominantWinWhenCorrect_ReturnsDominantWin()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.HomeWin,
            home: 5,
            away: 0,
            correct: true));

        Assert.Equal(BanterScenario.DominantWin, scenario);
    }

    [Fact]
    public void Classify_MissingScores_StillClassifiesFromOutcomes()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(Ctx(
            PredictionOutcomeKind.HomeWin,
            MatchOutcomeKind.HomeWin,
            home: null,
            away: null,
            correct: true));

        Assert.Equal(BanterScenario.SmugWinner, scenario);
    }

    [Fact]
    public void Classify_NewsWithoutMatchFacts_ReturnsGenericNews()
    {
        var scenario = DeterministicBanterScenarioClassifier.Classify(
            new BanterContext(
                null, null, null, null, null, null,
                PredictionOutcomeKind.Unknown,
                MatchOutcomeKind.Unknown,
                null, null, null,
                Category: "news"));

        Assert.Equal(BanterScenario.GenericNews, scenario);
    }

    private static BanterContext Ctx(
        PredictionOutcomeKind predicted,
        MatchOutcomeKind actual,
        int? home,
        int? away,
        bool? correct) =>
        new(
            UserId: null,
            PredictionId: null,
            MatchId: "m1",
            TeamId: null,
            TeamName: "Arsenal",
            OpponentName: "Chelsea",
            PredictedOutcome: predicted,
            ActualOutcome: actual,
            HomeScore: home,
            AwayScore: away,
            MatchFinishedAtUtc: DateTimeOffset.UtcNow,
            PredictionCorrect: correct);
}
