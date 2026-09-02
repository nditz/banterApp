namespace BanterApp.Api.Integrations.Banter;

/// <summary>Helpers for mapping feed/job inputs into Strategy Engine requests.</summary>
public static class BanterContextFactory
{
    public static BanterContext FromFeedItem(
        string? matchId,
        string? headline,
        string? summary,
        string? category,
        string? moodHint,
        string? teamName = null,
        string? opponentName = null,
        Guid? userId = null,
        Guid? predictionId = null,
        string? teamId = null,
        PredictionOutcomeKind predicted = PredictionOutcomeKind.Unknown,
        MatchOutcomeKind actual = MatchOutcomeKind.Unknown,
        int? homeScore = null,
        int? awayScore = null,
        bool? predictionCorrect = null) =>
        new(
            UserId: userId,
            PredictionId: predictionId,
            MatchId: matchId,
            TeamId: teamId,
            TeamName: teamName,
            OpponentName: opponentName,
            PredictedOutcome: predicted,
            ActualOutcome: actual,
            HomeScore: homeScore,
            AwayScore: awayScore,
            MatchFinishedAtUtc: null,
            Headline: headline,
            Summary: summary,
            Category: category,
            MoodHint: moodHint,
            PredictionCorrect: predictionCorrect);

    public static BanterGenerationRequest CreateRequest(
        BanterContext context,
        IEnumerable<string?>? suggestedQueries,
        string? mood,
        int seed) =>
        new(context, suggestedQueries?.ToList(), mood, seed);
}
