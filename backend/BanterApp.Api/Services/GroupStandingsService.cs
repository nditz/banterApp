using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public static class GroupStandingsService
{
    private sealed class TeamAccumulator
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public int Played;
        public int Won;
        public int Drawn;
        public int Lost;
        public int GoalsFor;
        public int GoalsAgainst;
        public int Points;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<GroupStandingEntry>> ComputeStandings(
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picksBySlotId)
    {
        var tables = new Dictionary<string, Dictionary<string, TeamAccumulator>>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in groupMatches.Where(m => !string.IsNullOrWhiteSpace(m.Group)))
        {
            var group = match.Group.Trim().ToUpperInvariant();
            if (!tables.TryGetValue(group, out var teams))
            {
                teams = new Dictionary<string, TeamAccumulator>(StringComparer.OrdinalIgnoreCase);
                tables[group] = teams;
            }

            EnsureTeam(teams, match.TeamACode, match.TeamA);
            EnsureTeam(teams, match.TeamBCode, match.TeamB);

            if (TryGetResult(match, picksBySlotId, out var homeGoals, out var awayGoals))
            {
                ApplyResult(teams[match.TeamACode.ToUpperInvariant()], homeGoals, awayGoals, isHome: true);
                ApplyResult(teams[match.TeamBCode.ToUpperInvariant()], awayGoals, homeGoals, isHome: false);
            }
        }

        return tables.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<GroupStandingEntry>)RankGroup(kvp.Value.Values),
            StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsGroupComplete(
        string group,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picksBySlotId)
    {
        var matches = groupMatches
            .Where(m => string.Equals(m.Group, group, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return matches.Count > 0 && matches.All(m =>
            MatchLockService.IsLocked(m) ||
            picksBySlotId.ContainsKey(BracketEngine.GroupSlotId(m.Id)));
    }

    public static BracketTeamInfo? GetQualifier(
        string group,
        int rank,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picksBySlotId)
    {
        if (!IsGroupComplete(group, groupMatches, picksBySlotId))
        {
            return null;
        }

        var standings = ComputeStandings(groupMatches, picksBySlotId);
        if (!standings.TryGetValue(group.Trim().ToUpperInvariant(), out var table))
        {
            return null;
        }

        var entry = table.FirstOrDefault(row => row.Rank == rank);
        return entry is null ? null : new BracketTeamInfo(entry.TeamCode, entry.TeamName);
    }

    private static bool TryGetResult(
        Match match,
        IReadOnlyDictionary<string, string> picksBySlotId,
        out int homeGoals,
        out int awayGoals)
    {
        homeGoals = 0;
        awayGoals = 0;

        if (match.Status == "FT" && match.HomeScore is not null && match.AwayScore is not null)
        {
            homeGoals = match.HomeScore.Value;
            awayGoals = match.AwayScore.Value;
            return true;
        }

        var slotId = BracketEngine.GroupSlotId(match.Id);
        if (!picksBySlotId.TryGetValue(slotId, out var winnerCode))
        {
            return false;
        }

        if (string.Equals(winnerCode, "DRAW", StringComparison.OrdinalIgnoreCase))
        {
            homeGoals = 1;
            awayGoals = 1;
            return true;
        }

        if (string.Equals(match.TeamACode, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            homeGoals = 1;
            awayGoals = 0;
            return true;
        }

        if (string.Equals(match.TeamBCode, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            homeGoals = 0;
            awayGoals = 1;
            return true;
        }

        return false;
    }

    private static void EnsureTeam(
        Dictionary<string, TeamAccumulator> teams,
        string code,
        string name)
    {
        var key = code.ToUpperInvariant();
        if (!teams.ContainsKey(key))
        {
            teams[key] = new TeamAccumulator { Code = code.ToUpperInvariant(), Name = name };
        }
    }

    private static void ApplyResult(TeamAccumulator team, int goalsFor, int goalsAgainst, bool isHome)
    {
        _ = isHome;
        team.Played++;
        team.GoalsFor += goalsFor;
        team.GoalsAgainst += goalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            team.Won++;
            team.Points += 3;
        }
        else if (goalsFor < goalsAgainst)
        {
            team.Lost++;
        }
        else
        {
            team.Drawn++;
            team.Points += 1;
        }
    }

    private static List<GroupStandingEntry> RankGroup(IEnumerable<TeamAccumulator> teams)
    {
        return teams
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalsFor - t.GoalsAgainst)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => new GroupStandingEntry(
                team.Code,
                team.Name,
                team.Played,
                team.Won,
                team.Drawn,
                team.Lost,
                team.GoalsFor,
                team.GoalsAgainst,
                team.GoalsFor - team.GoalsAgainst,
                team.Points,
                index + 1))
            .ToList();
    }
}
