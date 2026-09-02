namespace BanterApp.Api.Integrations.Banter;

/// <summary>
/// Deterministic scenario classification from match/prediction facts.
/// AI may refine tone later; it is never the sole source of truth.
/// </summary>
public sealed class DeterministicBanterScenarioClassifier : IBanterScenarioClassifier
{
    public Task<BanterScenario> ClassifyAsync(
        BanterContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Classify(context));
    }

    public static BanterScenario Classify(BanterContext context)
    {
        var predicted = context.PredictedOutcome;
        var actual = context.ActualOutcome;
        var correct = context.PredictionCorrect ??
                      (predicted != PredictionOutcomeKind.Unknown &&
                       actual != MatchOutcomeKind.Unknown &&
                       Matches(predicted, actual));

        var goalDiff = GoalDifference(context);
        var category = context.Category?.Trim().ToLowerInvariant();

        if (actual == MatchOutcomeKind.Unknown &&
            predicted == PredictionOutcomeKind.Unknown &&
            string.IsNullOrWhiteSpace(context.TeamName))
        {
            return category switch
            {
                "match_result" => BanterScenario.GenericWin,
                "match_live" => BanterScenario.LastMinuteDrama,
                "pundit_quote" => BanterScenario.RivalMockery,
                _ => BanterScenario.GenericNews
            };
        }

        if (actual == MatchOutcomeKind.Draw)
        {
            return correct ? BanterScenario.PredictionNailedIt : BanterScenario.DrawFrustration;
        }

        if (goalDiff is >= 3)
        {
            if (correct)
            {
                return BanterScenario.DominantWin;
            }

            return predicted != PredictionOutcomeKind.Unknown
                ? BanterScenario.Overconfidence
                : BanterScenario.DominantWin;
        }

        if (goalDiff is <= -3)
        {
            return correct
                ? BanterScenario.Heartbreak
                : BanterScenario.PredictionAgedBadly;
        }

        if (correct)
        {
            if (predicted == PredictionOutcomeKind.Draw)
            {
                return BanterScenario.PredictionNailedIt;
            }

            return goalDiff is 1
                ? BanterScenario.LuckyEscape
                : BanterScenario.SmugWinner;
        }

        if (predicted != PredictionOutcomeKind.Unknown &&
            actual != MatchOutcomeKind.Unknown)
        {
            // Predicted loss/draw but team won → underdog / nailed opposite
            if (IsWinForTeamPerspective(actual) &&
                predicted is PredictionOutcomeKind.AwayWin or PredictionOutcomeKind.Draw)
            {
                return BanterScenario.UnderdogUpset;
            }

            return BanterScenario.PredictionAgedBadly;
        }

        return actual switch
        {
            MatchOutcomeKind.HomeWin or MatchOutcomeKind.AwayWin => BanterScenario.GenericWin,
            MatchOutcomeKind.Draw => BanterScenario.GenericDraw,
            _ => BanterScenario.GenericNews
        };
    }

    private static bool Matches(PredictionOutcomeKind predicted, MatchOutcomeKind actual) =>
        (predicted, actual) switch
        {
            (PredictionOutcomeKind.HomeWin, MatchOutcomeKind.HomeWin) => true,
            (PredictionOutcomeKind.AwayWin, MatchOutcomeKind.AwayWin) => true,
            (PredictionOutcomeKind.Draw, MatchOutcomeKind.Draw) => true,
            _ => false
        };

    private static bool IsWinForTeamPerspective(MatchOutcomeKind actual) =>
        actual is MatchOutcomeKind.HomeWin or MatchOutcomeKind.AwayWin;

    private static int? GoalDifference(BanterContext context)
    {
        if (!context.HomeScore.HasValue || !context.AwayScore.HasValue)
        {
            return null;
        }

        // Prefer team perspective when TeamName matches home/away naming is unavailable:
        // use signed home-away for home-biased context; magnitude still drives Dominant/Heavy.
        return Math.Abs(context.HomeScore.Value - context.AwayScore.Value) *
               Math.Sign(context.HomeScore.Value - context.AwayScore.Value);
    }
}
