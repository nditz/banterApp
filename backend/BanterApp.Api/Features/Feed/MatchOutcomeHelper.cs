using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Features.Feed;

internal static class MatchOutcomeHelper
{
    private static readonly HashSet<string> FinishedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "FT", "FINISHED", "AET", "PEN", "FULL_TIME"
    };

    public static bool IsFinished(Match match) =>
        FinishedStatuses.Contains(match.Status) ||
        (match.HomeScore.HasValue && match.AwayScore.HasValue && match.KickoffTime <= DateTimeOffset.UtcNow);

    public static string? ResolveOutcome(Match match)
    {
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
        {
            return null;
        }

        if (match.HomeScore > match.AwayScore)
        {
            return "home";
        }

        if (match.HomeScore < match.AwayScore)
        {
            return "away";
        }

        return "draw";
    }

    public static string FormatScoreline(Match match)
    {
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue)
        {
            return "Full time — score TBC";
        }

        var outcome = ResolveOutcome(match) switch
        {
            "home" => "Home win",
            "away" => "Away win",
            _ => "Draw",
        };

        return $"{match.TeamA} {match.HomeScore}–{match.AwayScore} {match.TeamB} ({outcome})";
    }

    public static bool PunditHit(string prediction, Match match)
    {
        var outcome = ResolveOutcome(match);
        if (outcome is null)
        {
            return false;
        }

        var normalized = prediction.Trim().ToLowerInvariant();
        return outcome switch
        {
            "home" => normalized.Contains("home") || normalized.Contains(match.TeamA.ToLowerInvariant()),
            "away" => normalized.Contains("away") || normalized.Contains(match.TeamB.ToLowerInvariant()),
            "draw" => normalized.Contains("draw"),
            _ => false,
        };
    }

    public static string FormatUserPick(Prediction prediction, Match match) =>
        prediction.PredictionType switch
        {
            PredictionType.Result => prediction.PredictionValue switch
            {
                "home" => $"{match.TeamA} to win",
                "away" => $"{match.TeamB} to win",
                "draw" => "Draw",
                _ => prediction.PredictionValue,
            },
            PredictionType.CorrectScore => $"Correct score {prediction.PredictionValue}",
            PredictionType.DoubleChance => prediction.PredictionValue.Replace('_', ' '),
            _ => prediction.PredictionValue,
        };
}
