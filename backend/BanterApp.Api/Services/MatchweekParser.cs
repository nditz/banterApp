using System.Text.RegularExpressions;

namespace BanterApp.Api.Services;

public static class MatchweekParser
{
    private static readonly Regex RegularSeason = new(
        @"Regular Season\s*[-–]\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MatchweekLabel = new(
        @"(?:Matchweek|Matchday|Gameweek|Week)\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TrailingNumber = new(@"(\d+)\s*$", RegexOptions.Compiled);

    public static int? TryParse(string? round)
    {
        if (string.IsNullOrWhiteSpace(round))
        {
            return null;
        }

        var match = RegularSeason.Match(round);
        if (match.Success && TryWeek(match.Groups[1].Value, out var week))
        {
            return week;
        }

        match = MatchweekLabel.Match(round);
        if (match.Success && TryWeek(match.Groups[1].Value, out week))
        {
            return week;
        }

        match = TrailingNumber.Match(round.Trim());
        if (match.Success && TryWeek(match.Groups[1].Value, out week))
        {
            return week;
        }

        return null;
    }

    private static bool TryWeek(string raw, out int week)
    {
        week = 0;
        return int.TryParse(raw, out week) && week is >= 1 and <= 38;
    }
}
