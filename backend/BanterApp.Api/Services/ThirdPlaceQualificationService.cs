using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

/// <summary>
/// Determines which 8 of 12 third-placed teams advance and their Annex C R32 assignments.
/// Ranking criteria (FIFA 2026 group stage, applied across all third-placed teams):
/// 1. Points 2. Goal difference 3. Goals scored 4. Team name (stand-in when fair-play is unavailable).
/// </summary>
public static class ThirdPlaceQualificationService
{
    public const string RulesSummary =
        "Top two in each group (24 teams) qualify automatically. The eight best third-placed teams " +
        "from the remaining 12 are ranked on points, then goal difference, then goals scored. " +
        "FIFA Annex C then assigns each qualifying third-placed team to a fixed Round of 32 slot " +
        "based on which eight groups produced qualifiers — 495 combinations are pre-defined.";

    public static readonly IReadOnlyList<string> RankingCriteria =
    [
        "Points in the group stage",
        "Goal difference in the group stage",
        "Goals scored in the group stage",
        "Fair play points (not tracked here — alphabetical team name used as final tiebreak)"
    ];

    public static ThirdPlaceQualificationSnapshot ComputeSnapshot(
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picksBySlotId)
    {
        var allGroups = groupMatches
            .Where(m => !string.IsNullOrWhiteSpace(m.Group))
            .Select(m => m.Group.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.Ordinal)
            .ToList();

        var completeGroups = allGroups
            .Where(g => GroupStandingsService.IsGroupComplete(g, groupMatches, picksBySlotId))
            .ToList();

        var standings = GroupStandingsService.ComputeStandings(groupMatches, picksBySlotId);
        var thirdPlaceRows = standings
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp =>
            {
                var third = kvp.Value.FirstOrDefault(row => row.Rank == 3);
                return third is null
                    ? null
                    : new ThirdPlaceCandidate(
                        kvp.Key,
                        third.TeamCode,
                        third.TeamName,
                        third.Points,
                        third.GoalDifference,
                        third.GoalsFor,
                        GroupComplete: completeGroups.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase));
            })
            .Where(row => row is not null)
            .Cast<ThirdPlaceCandidate>()
            .ToList();

        var rankedKnown = thirdPlaceRows
            .Where(t => t.GroupComplete)
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.TeamName, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => team with { RankAmongThirds = index + 1 })
            .ToList();

        var rankedLookup = rankedKnown.ToDictionary(t => t.Group, StringComparer.OrdinalIgnoreCase);
        var displayRows = thirdPlaceRows
            .Select(t => rankedLookup.TryGetValue(t.Group, out var ranked) ? ranked : t)
            .OrderBy(t => t.RankAmongThirds is > 0 ? t.RankAmongThirds : 99)
            .ThenBy(t => t.Group, StringComparer.Ordinal)
            .ToList();

        var allComplete = completeGroups.Count == 12 && thirdPlaceRows.Count == 12;
        if (!allComplete)
        {
            return new ThirdPlaceQualificationSnapshot(
                RulesSummary,
                RankingCriteria,
                completeGroups.Count,
                12,
                displayRows,
                [],
                null,
                null,
                false,
                false);
        }

        var ranked = thirdPlaceRows
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.TeamName, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => team with { RankAmongThirds = index + 1, GroupComplete = true })
            .ToList();

        var qualified = ranked.Take(8).ToList();
        var combinationKey = AnnexCMatrix.BuildCombinationKey(qualified.Select(q => q.Group));

        if (!AnnexCMatrix.TryGetAssignment(combinationKey, out var annexMapping))
        {
            return new ThirdPlaceQualificationSnapshot(
                RulesSummary,
                RankingCriteria,
                12,
                12,
                ranked,
                qualified,
                combinationKey,
                null,
                true,
                false);
        }

        var slotAssignments = AnnexCMatrix.GroupWinnerSlotKeys
            .ToDictionary(
                slot => slot,
                slot =>
                {
                    if (!annexMapping.TryGetValue(slot, out var thirdCode))
                    {
                        return (string?)null;
                    }

                    return AnnexCMatrix.ParseThirdGroupLetter(thirdCode);
                },
                StringComparer.OrdinalIgnoreCase);

        return new ThirdPlaceQualificationSnapshot(
            RulesSummary,
            RankingCriteria,
            12,
            12,
            ranked,
            qualified,
            combinationKey,
            slotAssignments,
            true,
            true);
    }

    public static IReadOnlyDictionary<string, BracketTeamInfo> BuildBracketAssignments(
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picksBySlotId)
    {
        var state = ComputeSnapshot(groupMatches, picksBySlotId);
        if (!state.IsComplete || state.SlotAssignments is null)
        {
            return new Dictionary<string, BracketTeamInfo>(StringComparer.OrdinalIgnoreCase);
        }

        var assignments = new Dictionary<string, BracketTeamInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var slot in BracketTemplate.KnockoutSlots
                     .Where(s => string.Equals(s.Round, "Round of 32", StringComparison.OrdinalIgnoreCase)))
        {
            if (slot.TeamSourceB is not AnnexCThirdSource annex)
            {
                continue;
            }

            var annexKey = $"1{annex.GroupWinnerLetter}";
            if (!state.SlotAssignments.TryGetValue(annexKey, out var thirdGroup) ||
                string.IsNullOrWhiteSpace(thirdGroup))
            {
                continue;
            }

            var team = GroupStandingsService.GetQualifier(thirdGroup, 3, groupMatches, picksBySlotId);
            if (team is not null)
            {
                assignments[$"{slot.SlotId}:B"] = team;
            }
        }

        return assignments;
    }
}

public sealed record ThirdPlaceCandidate(
    string Group,
    string TeamCode,
    string TeamName,
    int Points,
    int GoalDifference,
    int GoalsFor,
    int RankAmongThirds = 0,
    bool GroupComplete = false)
{
    public bool Qualified => RankAmongThirds is > 0 and <= 8;
}

public sealed record ThirdPlaceQualificationSnapshot(
    string RulesSummary,
    IReadOnlyList<string> RankingCriteria,
    int GroupsComplete,
    int TotalGroups,
    IReadOnlyList<ThirdPlaceCandidate> AllThirdPlaceTeams,
    IReadOnlyList<ThirdPlaceCandidate> QualifiedTeams,
    string? CombinationKey,
    IReadOnlyDictionary<string, string?>? SlotAssignments,
    bool IsComplete,
    bool AnnexCResolved);
