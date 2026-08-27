using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Services;

namespace BanterApp.Api.Features.Matches;

/// <summary>
/// Builds a Premier League table from finished fixtures so the public table
/// stays in sync even when the upstream standings endpoint is stale or rate-limited.
/// </summary>
public static class PremierLeagueStandingsCalculator
{
    public static IReadOnlyList<StandingRowResponse> FromMatches(IEnumerable<Match> matches)
    {
        var teams = new Dictionary<string, TeamRow>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches)
        {
            Ensure(teams, match.TeamACode, match.TeamA, match.HomeLogoUrl);
            Ensure(teams, match.TeamBCode, match.TeamB, match.AwayLogoUrl);
        }

        foreach (var match in matches.Where(m => CurrentMatchweek.IsFinished(m.Status)))
        {
            if (!teams.TryGetValue(match.TeamACode, out var home) ||
                !teams.TryGetValue(match.TeamBCode, out var away))
            {
                continue;
            }

            var homeGoals = match.HomeScore ?? 0;
            var awayGoals = match.AwayScore ?? 0;
            ApplyResult(ref home, homeGoals, awayGoals);
            ApplyResult(ref away, awayGoals, homeGoals);
            teams[match.TeamACode] = home;
            teams[match.TeamBCode] = away;
        }

        return PremierLeagueTableRanking.Rank(teams.Values.Select(row => new StandingRowResponse(
            0,
            row.Code,
            row.Name,
            ClubBadges.Coalesce(row.LogoUrl, row.Code, row.Name),
            row.Played,
            row.Won,
            row.Drawn,
            row.Lost,
            row.GoalsFor,
            row.GoalsAgainst,
            row.GoalsFor - row.GoalsAgainst,
            row.Points)));
    }

    private static void Ensure(
        Dictionary<string, TeamRow> teams,
        string code,
        string name,
        string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(code) || teams.ContainsKey(code))
        {
            return;
        }

        teams[code] = new TeamRow(code, name, logoUrl, 0, 0, 0, 0, 0, 0, 0);
    }

    private static void ApplyResult(ref TeamRow row, int scored, int conceded)
    {
        row.Played++;
        row.GoalsFor += scored;
        row.GoalsAgainst += conceded;
        if (scored > conceded)
        {
            row.Won++;
            row.Points += 3;
        }
        else if (scored < conceded)
        {
            row.Lost++;
        }
        else
        {
            row.Drawn++;
            row.Points++;
        }
    }

    private struct TeamRow(
        string code,
        string name,
        string? logoUrl,
        int played,
        int won,
        int drawn,
        int lost,
        int goalsFor,
        int goalsAgainst,
        int points)
    {
        public string Code { get; } = code;
        public string Name { get; } = name;
        public string? LogoUrl { get; } = logoUrl;
        public int Played = played;
        public int Won = won;
        public int Drawn = drawn;
        public int Lost = lost;
        public int GoalsFor = goalsFor;
        public int GoalsAgainst = goalsAgainst;
        public int Points = points;
    }
}
