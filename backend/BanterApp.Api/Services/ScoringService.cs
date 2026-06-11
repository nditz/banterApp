using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public sealed class ScoringService
{
    public const int CorrectResultPoints = 3;
    public const int CorrectScorePoints = 7;
    public const int DoubleChancePoints = 2;
    public const int PerfectMatchDayBonus = 5;
    public const int PerfectGroupStageBonus = 20;

    public int CalculatePoints(PredictionType type, string predictionValue, Match match)
    {
        if (match.Status != "FT" || match.HomeScore is null || match.AwayScore is null)
        {
            return 0;
        }

        return type switch
        {
            PredictionType.Result => IsCorrectResult(predictionValue, match) ? CorrectResultPoints : 0,
            PredictionType.CorrectScore => IsCorrectScore(predictionValue, match) ? CorrectScorePoints : 0,
            PredictionType.DoubleChance => IsCorrectDoubleChance(predictionValue, match) ? DoubleChancePoints : 0,
            _ => 0
        };
    }

    public int CalculatePerfectMatchDayBonus(IReadOnlyList<Prediction> dayPredictions, IReadOnlyList<Match> matches)
    {
        if (dayPredictions.Count == 0)
        {
            return 0;
        }

        var matchLookup = matches.ToDictionary(m => m.Id);
        var allCorrect = dayPredictions.All(p =>
        {
            if (!matchLookup.TryGetValue(p.MatchId, out var match))
            {
                return false;
            }

            return CalculatePoints(p.PredictionType, p.PredictionValue, match) > 0;
        });

        return allCorrect ? PerfectMatchDayBonus : 0;
    }

    public int CalculatePerfectGroupStageBonus(IReadOnlyList<Prediction> groupPredictions, IReadOnlyList<Match> groupMatches)
    {
        if (groupPredictions.Count == 0 || groupMatches.Count == 0)
        {
            return 0;
        }

        var finished = groupMatches.Where(m => m.Status == "FT").ToList();
        if (finished.Count == 0)
        {
            return 0;
        }

        var predictionsByMatch = groupPredictions
            .Where(p => p.PredictionType == PredictionType.Result)
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.First());

        var allGroupResultsCorrect = finished.All(match =>
        {
            if (!predictionsByMatch.TryGetValue(match.Id, out var prediction))
            {
                return false;
            }

            return IsCorrectResult(prediction.PredictionValue, match);
        });

        return allGroupResultsCorrect ? PerfectGroupStageBonus : 0;
    }

    public static string ResolveMatchResult(Match match)
    {
        if (match.HomeScore is null || match.AwayScore is null)
        {
            return string.Empty;
        }

        if (match.HomeScore > match.AwayScore)
        {
            return "H";
        }

        if (match.HomeScore < match.AwayScore)
        {
            return "A";
        }

        return "D";
    }

    private static bool IsCorrectResult(string predictionValue, Match match)
    {
        var actual = ResolveMatchResult(match);
        var normalized = NormalizeResult(predictionValue);
        return !string.IsNullOrEmpty(actual) && string.Equals(normalized, actual, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCorrectScore(string predictionValue, Match match)
    {
        if (!TryParseScore(predictionValue, out var home, out var away))
        {
            return false;
        }

        return match.HomeScore == home && match.AwayScore == away;
    }

    private static bool IsCorrectDoubleChance(string predictionValue, Match match)
    {
        var actual = ResolveMatchResult(match);
        if (string.IsNullOrEmpty(actual))
        {
            return false;
        }

        var normalized = predictionValue.Trim().ToUpperInvariant() switch
        {
            "HOME OR DRAW" or "H/D" or "HD" or "1X" => "HD",
            "AWAY OR DRAW" or "A/D" or "DA" or "X2" => "DA",
            "HOME OR AWAY" or "H/A" or "HA" or "12" => "HA",
            _ => predictionValue.Trim().ToUpperInvariant()
        };

        return normalized switch
        {
            "HD" => actual is "H" or "D",
            "DA" => actual is "D" or "A",
            "HA" => actual is "H" or "A",
            _ => false
        };
    }

    private static string NormalizeResult(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "HOME" or "HOME WIN" or "H" or "1" => "H",
            "DRAW" or "D" or "X" => "D",
            "AWAY" or "AWAY WIN" or "A" or "2" => "A",
            _ => value.Trim().ToUpperInvariant()
        };

    private static bool TryParseScore(string value, out int home, out int away)
    {
        home = 0;
        away = 0;
        var parts = value.Split('-', ':');
        return parts.Length == 2 &&
               int.TryParse(parts[0].Trim(), out home) &&
               int.TryParse(parts[1].Trim(), out away);
    }
}
