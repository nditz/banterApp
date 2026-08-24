namespace BanterApp.Api.Features.Matches;

/// <summary>
/// Premier League table order during a season (Rules C.4–C.6):
/// points, then goal difference, then goals scored.
/// </summary>
public static class PremierLeagueTableRanking
{
    public static IReadOnlyList<StandingRowResponse> Rank(IEnumerable<StandingRowResponse> rows)
    {
        return rows
            .GroupBy(r => r.TeamCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(r => (Row: r, Key: (r.Points, r.GoalDiff, r.GoalsFor, r.TeamName)))
            .OrderByDescending(x => x.Key.Points)
            .ThenByDescending(x => x.Key.GoalDiff)
            .ThenByDescending(x => x.Key.GoalsFor)
            .ThenBy(x => x.Key.TeamName, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => x.Row with { Rank = index + 1 })
            .ToList();
    }
}
