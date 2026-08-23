using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public sealed class ScoringService
{
    public const int CorrectResultPoints = 3;
    public const int CorrectScorePoints = 7;
    public const int DoubleChancePoints = 2;
    public const int PerfectMatchweekBonus = 5;

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

    public int CalculatePerfectMatchweekBonus(IReadOnlyList<Prediction> weekPredictions, IReadOnlyList<Match> matches)
    {
        if (weekPredictions.Count == 0 || matches.Count == 0)
        {
            return 0;
        }

        var matchLookup = matches.ToDictionary(m => m.Id);
        var resultPicks = weekPredictions
            .Where(p => p.PredictionType == PredictionType.Result)
            .GroupBy(p => p.MatchId)
            .ToDictionary(g => g.Key, g => g.First());

        if (resultPicks.Count < matches.Count)
        {
            return 0;
        }

        var allCorrect = matches.All(match =>
        {
            if (!resultPicks.TryGetValue(match.Id, out var prediction))
            {
                return false;
            }

            return CalculatePoints(prediction.PredictionType, prediction.PredictionValue, match) > 0;
        });

        return allCorrect ? PerfectMatchweekBonus : 0;
    }

    public int CalculatePerfectMatchDayBonus(IReadOnlyList<Prediction> dayPredictions, IReadOnlyList<Match> matches) =>
        CalculatePerfectMatchweekBonus(dayPredictions, matches);

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
