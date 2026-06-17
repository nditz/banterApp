namespace BanterApp.Api.Services;

public static class BracketTemplate
{
    /// <summary>
    /// Knockout phase for the 48-team 2026 World Cup (OpenFootball match nums 73–104).
    /// Group-stage slots are generated from fixtures at runtime.
    /// </summary>
    public static IReadOnlyList<BracketSlotDefinition> KnockoutSlots { get; } =
    [
        // Round of 32
        R32("r32-1", 73, 1, new GroupRankSource("A", 2), new GroupRankSource("B", 2)),
        R32("r32-2", 74, 2, new GroupRankSource("E", 1), new AnnexCThirdSource("E")),
        R32("r32-3", 75, 3, new GroupRankSource("F", 1), new GroupRankSource("C", 2)),
        R32("r32-4", 76, 4, new GroupRankSource("C", 1), new GroupRankSource("F", 2)),
        R32("r32-5", 77, 5, new GroupRankSource("I", 1), new AnnexCThirdSource("I")),
        R32("r32-6", 78, 6, new GroupRankSource("E", 2), new GroupRankSource("I", 2)),
        R32("r32-7", 79, 7, new GroupRankSource("A", 1), new AnnexCThirdSource("A")),
        R32("r32-8", 80, 8, new GroupRankSource("L", 1), new AnnexCThirdSource("L")),
        R32("r32-9", 81, 9, new GroupRankSource("D", 1), new AnnexCThirdSource("D")),
        R32("r32-10", 82, 10, new GroupRankSource("G", 1), new AnnexCThirdSource("G")),
        R32("r32-11", 83, 11, new GroupRankSource("K", 2), new GroupRankSource("L", 2)),
        R32("r32-12", 84, 12, new GroupRankSource("H", 1), new GroupRankSource("J", 2)),
        R32("r32-13", 85, 13, new GroupRankSource("B", 1), new AnnexCThirdSource("B")),
        R32("r32-14", 86, 14, new GroupRankSource("J", 1), new GroupRankSource("H", 2)),
        R32("r32-15", 87, 15, new GroupRankSource("K", 1), new AnnexCThirdSource("K")),
        R32("r32-16", 88, 16, new GroupRankSource("D", 2), new GroupRankSource("G", 2)),

        // Round of 16
        Bracket("r16-1", 89, "Round of 16", 2, 1, "r32-2", "r32-5"),
        Bracket("r16-2", 90, "Round of 16", 2, 2, "r32-1", "r32-3"),
        Bracket("r16-3", 91, "Round of 16", 2, 3, "r32-4", "r32-6"),
        Bracket("r16-4", 92, "Round of 16", 2, 4, "r32-7", "r32-8"),
        Bracket("r16-5", 93, "Round of 16", 2, 5, "r32-11", "r32-12"),
        Bracket("r16-6", 94, "Round of 16", 2, 6, "r32-9", "r32-10"),
        Bracket("r16-7", 95, "Round of 16", 2, 7, "r32-14", "r32-16"),
        Bracket("r16-8", 96, "Round of 16", 2, 8, "r32-13", "r32-15"),

        // Quarter-finals
        Bracket("qf-1", 97, "Quarter-finals", 3, 1, "r16-1", "r16-2"),
        Bracket("qf-2", 98, "Quarter-finals", 3, 2, "r16-5", "r16-6"),
        Bracket("qf-3", 99, "Quarter-finals", 3, 3, "r16-3", "r16-4"),
        Bracket("qf-4", 100, "Quarter-finals", 3, 4, "r16-7", "r16-8"),

        // Semi-finals
        Bracket("sf-1", 101, "Semi-finals", 4, 1, "qf-1", "qf-2"),
        Bracket("sf-2", 102, "Semi-finals", 4, 2, "qf-3", "qf-4"),

        // Third-place play-off & Final
        Bracket("third", 103, "Third-place play-off", 5, 1, "sf-1", "sf-2", losers: true),
        Bracket("final", 104, "Final", 6, 1, "sf-1", "sf-2"),
    ];

    public static IReadOnlyDictionary<string, BracketSlotDefinition> BySlotId { get; } =
        KnockoutSlots.ToDictionary(s => s.SlotId, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> DownstreamSlotIds(string slotId)
    {
        var queue = new Queue<string>();
        queue.Enqueue(slotId);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var slot in KnockoutSlots)
            {
                if (visited.Contains(slot.SlotId))
                {
                    continue;
                }

                if (ReferencesSlot(slot.TeamSourceA, current) ||
                    ReferencesSlot(slot.TeamSourceB, current))
                {
                    visited.Add(slot.SlotId);
                    queue.Enqueue(slot.SlotId);
                }
            }
        }

        return visited;
    }

    public static IEnumerable<string> KnockoutSlotsAffectedByGroup(string group)
    {
        var normalized = group.Trim().ToUpperInvariant();
        return KnockoutSlots
            .Where(slot => SourceReferencesGroup(slot.TeamSourceA, normalized) ||
                           SourceReferencesGroup(slot.TeamSourceB, normalized))
            .Select(slot => slot.SlotId);
    }

    public static string FormatTeamSource(TeamSource? source) =>
        source switch
        {
            GroupRankSource g => g.Rank switch
            {
                1 => $"1{g.Group}",
                2 => $"2{g.Group}",
                3 => $"3{g.Group}",
                _ => $"{g.Rank}{g.Group}"
            },
            ThirdPlaceSource t => $"3{string.Join("/", t.CandidateGroups)}",
            AnnexCThirdSource a => $"3?→1{a.GroupWinnerLetter}",
            SlotWinnerSource w => $"W{WinnerLabel(w.SlotId)}",
            SlotLoserSource l => $"L{LoserLabel(l.SlotId)}",
            _ => "TBD"
        };

    private static string WinnerLabel(string slotId)
    {
        var slot = KnockoutSlots.FirstOrDefault(s =>
            string.Equals(s.SlotId, slotId, StringComparison.OrdinalIgnoreCase));
        if (slot is null)
        {
            return slotId;
        }

        var num = slot.MatchId["of26-ko-".Length..];
        return num;
    }

    private static string LoserLabel(string slotId) => WinnerLabel(slotId);

    private static bool ReferencesSlot(TeamSource? source, string slotId) =>
        source switch
        {
            SlotWinnerSource w => string.Equals(w.SlotId, slotId, StringComparison.OrdinalIgnoreCase),
            SlotLoserSource l => string.Equals(l.SlotId, slotId, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static bool SourceReferencesGroup(TeamSource? source, string group) =>
        source switch
        {
            GroupRankSource g => string.Equals(g.Group, group, StringComparison.OrdinalIgnoreCase),
            ThirdPlaceSource t => t.CandidateGroups.Any(g =>
                string.Equals(g, group, StringComparison.OrdinalIgnoreCase)),
            AnnexCThirdSource => true,
            _ => false
        };

    private static string Ko(int num) => $"of26-ko-{num}";

    private static BracketSlotDefinition R32(
        string slotId,
        int matchNum,
        int position,
        TeamSource sourceA,
        TeamSource sourceB) =>
        new(slotId, Ko(matchNum), "Round of 32", 1, position, BracketSlotKind.Knockout, sourceA, sourceB);

    private static BracketSlotDefinition Bracket(
        string slotId,
        int matchNum,
        string round,
        int roundOrder,
        int position,
        string sourceA,
        string sourceB,
        bool losers = false) =>
        new(
            slotId,
            Ko(matchNum),
            round,
            roundOrder,
            position,
            BracketSlotKind.Knockout,
            losers ? new SlotLoserSource(sourceA) : new SlotWinnerSource(sourceA),
            losers ? new SlotLoserSource(sourceB) : new SlotWinnerSource(sourceB));
}
