using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;

namespace BanterApp.Api.Features.Receipts;

public static class ReceiptStoryClassifier
{
    public readonly record struct PunditTake(
        Guid PunditId,
        string Name,
        string Prediction,
        string? SourceUrl,
        string? SourcePlatform,
        bool WasCorrect);

    public sealed record Classification(
        string Primary,
        IReadOnlyList<string> All,
        IReadOnlyList<(string Type, string Summary)> Candidates);

    public static Classification Classify(
        Prediction prediction,
        Match match,
        IReadOnlyList<PunditTake> punditTakes,
        int resultPickCount,
        int wrongResultPickCount)
    {
        var userHit = prediction.PointsAwarded > 0;
        var types = new List<string>();

        if (prediction.PredictionType == PredictionType.CorrectScore && userHit)
        {
            types.Add(ReceiptStoryTypes.ExactScore);
        }

        var punditHits = punditTakes.Count(t => t.WasCorrect);
        var punditMisses = punditTakes.Count(t => !t.WasCorrect);
        if (punditTakes.Count > 0 && userHit && punditMisses > 0)
        {
            types.Add(ReceiptStoryTypes.BeatPundit);
        }
        else if (punditTakes.Count > 0 && !userHit && punditHits > 0)
        {
            types.Add(ReceiptStoryTypes.PunditBeatUser);
        }

        if (resultPickCount >= 3 && wrongResultPickCount > resultPickCount / 2.0)
        {
            types.Add(userHit ? ReceiptStoryTypes.MinorityRight : ReceiptStoryTypes.MajorityWrong);
        }

        types.Add(userHit ? ReceiptStoryTypes.Hit : ReceiptStoryTypes.Miss);

        var primary = types[0];
        var scoreline = MatchOutcomeHelper.FormatScoreline(match);
        var candidates = types.Select((type, index) => (type, Summarize(type, prediction, match, punditTakes, scoreline))).ToList();
        return new Classification(primary, types, candidates);
    }

    public static string ResultHash(Match match) =>
        $"{match.Status}:{match.HomeScore?.ToString() ?? "x"}-{match.AwayScore?.ToString() ?? "x"}";

    private static string Summarize(
        string type,
        Prediction prediction,
        Match match,
        IReadOnlyList<PunditTake> punditTakes,
        string scoreline)
    {
        var beaten = punditTakes.FirstOrDefault(t => !t.WasCorrect);
        var beating = punditTakes.FirstOrDefault(t => t.WasCorrect);
        return type switch
        {
            ReceiptStoryTypes.ExactScore =>
                $"Exact score. {scoreline}.",
            ReceiptStoryTypes.BeatPundit when beaten.Name is { Length: > 0 } =>
                $"You beat {beaten.Name}'s sourced pick. {scoreline}.",
            ReceiptStoryTypes.BeatPundit =>
                $"You beat a sourced pundit pick. {scoreline}.",
            ReceiptStoryTypes.PunditBeatUser when beating.Name is { Length: > 0 } =>
                $"{beating.Name}'s sourced pick landed. {scoreline}.",
            ReceiptStoryTypes.PunditBeatUser =>
                $"A sourced pundit pick landed. {scoreline}.",
            ReceiptStoryTypes.MinorityRight =>
                $"You were in the minority that got it right. {scoreline}.",
            ReceiptStoryTypes.MajorityWrong =>
                $"Most result picks missed this one. {scoreline}.",
            ReceiptStoryTypes.Hit =>
                $"Your pick scored {prediction.PointsAwarded} pts. {scoreline}.",
            _ =>
                $"Your pick missed. {scoreline}."
        };
    }
}
