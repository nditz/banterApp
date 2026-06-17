using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Services;

public static class BracketEngine
{
    public static string GroupSlotId(string matchId) => $"grp-{matchId}";

    public static IReadOnlyList<BracketSlotDefinition> BuildGroupSlots(IReadOnlyList<Match> groupMatches)
    {
        return groupMatches
            .Where(m => !string.IsNullOrWhiteSpace(m.Group))
            .OrderBy(m => m.Group)
            .ThenBy(m => m.KickoffTime)
            .Select((match, index) => new BracketSlotDefinition(
                GroupSlotId(match.Id),
                match.Id,
                $"Group {match.Group}",
                0,
                index + 1,
                BracketSlotKind.GroupMatch,
                null,
                null))
            .ToList();
    }

    public static IReadOnlyList<BracketSlotDefinition> GetAllSlots(IReadOnlyList<Match> groupMatches) =>
        BuildGroupSlots(groupMatches).Concat(BracketTemplate.KnockoutSlots).ToList();

    public static bool TryGetSlot(string slotId, IReadOnlyList<Match> groupMatches, out BracketSlotDefinition slot)
    {
        slot = default!;
        if (BracketTemplate.BySlotId.TryGetValue(slotId, out var knockout))
        {
            slot = knockout;
            return true;
        }

        if (!slotId.StartsWith("grp-", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var matchId = slotId["grp-".Length..];
        var match = groupMatches.FirstOrDefault(m =>
            string.Equals(m.Id, matchId, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return false;
        }

        slot = BuildGroupSlots(groupMatches).First(s =>
            string.Equals(s.SlotId, slotId, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    public static IEnumerable<string> DownstreamSlotIds(string slotId, IReadOnlyList<Match> groupMatches)
    {
        if (slotId.StartsWith("grp-", StringComparison.OrdinalIgnoreCase))
        {
            var match = groupMatches.FirstOrDefault(m =>
                string.Equals(GroupSlotId(m.Id), slotId, StringComparison.OrdinalIgnoreCase));

            if (match is null || string.IsNullOrWhiteSpace(match.Group))
            {
                yield break;
            }

            foreach (var affected in BracketTemplate.KnockoutSlotsAffectedByGroup(match.Group))
            {
                yield return affected;
                foreach (var nested in BracketTemplate.DownstreamSlotIds(affected))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        foreach (var downstream in BracketTemplate.DownstreamSlotIds(slotId))
        {
            yield return downstream;
        }
    }

    public static (BracketTeamInfo? TeamA, BracketTeamInfo? TeamB, bool Ready) ResolveTeams(
        BracketSlotDefinition slot,
        IReadOnlyDictionary<string, Match> matches,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picks)
    {
        if (slot.Kind == BracketSlotKind.GroupMatch)
        {
            if (!matches.TryGetValue(slot.MatchId, out var match))
            {
                return (null, null, false);
            }

            return (
                new BracketTeamInfo(match.TeamACode, match.TeamA),
                new BracketTeamInfo(match.TeamBCode, match.TeamB),
                true);
        }

        var thirdPlaceAssignments = ThirdPlaceQualificationService.BuildBracketAssignments(groupMatches, picks);
        var teamA = ResolveTeamSource(slot.TeamSourceA, slot.SlotId, "A", matches, groupMatches, picks, thirdPlaceAssignments);
        var teamB = ResolveTeamSource(slot.TeamSourceB, slot.SlotId, "B", matches, groupMatches, picks, thirdPlaceAssignments);
        return (teamA, teamB, teamA is not null && teamB is not null);
    }

    private static BracketTeamInfo? ResolveTeamSource(
        TeamSource? source,
        string slotId,
        string side,
        IReadOnlyDictionary<string, Match> matches,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picks,
        IReadOnlyDictionary<string, BracketTeamInfo> thirdPlaceAssignments)
    {
        return source switch
        {
            GroupRankSource group => GroupStandingsService.GetQualifier(
                group.Group, group.Rank, groupMatches, picks),
            ThirdPlaceSource => thirdPlaceAssignments.TryGetValue($"{slotId}:{side}", out var third)
                ? third
                : null,
            AnnexCThirdSource => thirdPlaceAssignments.TryGetValue($"{slotId}:{side}", out var annexThird)
                ? annexThird
                : null,
            SlotWinnerSource winner => ResolveKnockoutWinner(
                winner.SlotId, matches, groupMatches, picks),
            SlotLoserSource loser => ResolveKnockoutLoser(
                loser.SlotId, matches, groupMatches, picks),
            _ => null
        };
    }

    private static BracketTeamInfo? ResolveKnockoutWinner(
        string sourceSlotId,
        IReadOnlyDictionary<string, Match> matches,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picks)
    {
        if (string.IsNullOrWhiteSpace(sourceSlotId) ||
            !BracketTemplate.BySlotId.TryGetValue(sourceSlotId, out var sourceSlot))
        {
            return null;
        }

        if (!picks.TryGetValue(sourceSlotId, out var winnerCode))
        {
            return null;
        }

        var (teamA, teamB, _) = ResolveTeams(sourceSlot, matches, groupMatches, picks);
        if (teamA is not null && string.Equals(teamA.Code, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            return teamA;
        }

        if (teamB is not null && string.Equals(teamB.Code, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            return teamB;
        }

        return null;
    }

    private static BracketTeamInfo? ResolveKnockoutLoser(
        string sourceSlotId,
        IReadOnlyDictionary<string, Match> matches,
        IReadOnlyList<Match> groupMatches,
        IReadOnlyDictionary<string, string> picks)
    {
        if (string.IsNullOrWhiteSpace(sourceSlotId) ||
            !BracketTemplate.BySlotId.TryGetValue(sourceSlotId, out var sourceSlot))
        {
            return null;
        }

        if (!picks.TryGetValue(sourceSlotId, out var winnerCode))
        {
            return null;
        }

        var (teamA, teamB, ready) = ResolveTeams(sourceSlot, matches, groupMatches, picks);
        if (!ready)
        {
            return null;
        }

        if (teamA is not null && string.Equals(teamA.Code, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            return teamB;
        }

        if (teamB is not null && string.Equals(teamB.Code, winnerCode, StringComparison.OrdinalIgnoreCase))
        {
            return teamA;
        }

        return null;
    }
}
