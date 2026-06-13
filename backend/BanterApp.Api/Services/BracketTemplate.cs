namespace BanterApp.Api.Services;

public static class BracketTemplate
{
    /// <summary>
    /// Knockout phase only. Group-stage slots are generated from fixtures at runtime.
    /// R16 pairings follow standard World Cup crossover (winner group X vs runner-up group Y).
    /// </summary>
    public static IReadOnlyList<BracketSlotDefinition> KnockoutSlots { get; } =
    [
        Knockout("r16-1", "wc26-r16-01", "Round of 16", 1, 1, new("A", 1), new("B", 2)),
        Knockout("r16-2", "wc26-r16-02", "Round of 16", 1, 2, new("C", 1), new("D", 2)),
        Knockout("r16-3", "wc26-r16-03", "Round of 16", 1, 3, new("E", 1), new("F", 2)),
        Knockout("r16-4", "wc26-r16-04", "Round of 16", 1, 4, new("G", 1), new("H", 2)),
        Knockout("r16-5", "wc26-r16-05", "Round of 16", 1, 5, new("B", 1), new("A", 2)),
        Knockout("r16-6", "wc26-r16-06", "Round of 16", 1, 6, new("D", 1), new("C", 2)),
        Knockout("r16-7", "wc26-r16-07", "Round of 16", 1, 7, new("F", 1), new("E", 2)),
        Knockout("r16-8", "wc26-r16-08", "Round of 16", 1, 8, new("H", 1), new("G", 2)),
        Bracket("qf-1", "wc26-qf-01", "Quarter-finals", 2, 1, "r16-1", "r16-2"),
        Bracket("qf-2", "wc26-qf-02", "Quarter-finals", 2, 2, "r16-3", "r16-4"),
        Bracket("qf-3", "wc26-qf-03", "Quarter-finals", 2, 3, "r16-5", "r16-6"),
        Bracket("qf-4", "wc26-qf-04", "Quarter-finals", 2, 4, "r16-7", "r16-8"),
        Bracket("sf-1", "wc26-sf-01", "Semi-finals", 3, 1, "qf-1", "qf-2"),
        Bracket("sf-2", "wc26-sf-02", "Semi-finals", 3, 2, "qf-3", "qf-4"),
        Bracket("final", "wc26-final", "Final", 4, 1, "sf-1", "sf-2"),
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

                if (string.Equals(slot.SourceSlotAId, current, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slot.SourceSlotBId, current, StringComparison.OrdinalIgnoreCase))
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
            .Where(slot =>
                string.Equals(slot.QualifierA?.Group, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(slot.QualifierB?.Group, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(slot => slot.SlotId);
    }

    private static BracketSlotDefinition Knockout(
        string slotId,
        string matchId,
        string round,
        int roundOrder,
        int position,
        GroupQualifierRef qualifierA,
        GroupQualifierRef qualifierB) =>
        new(slotId, matchId, round, roundOrder, position, BracketSlotKind.Knockout, null, null, qualifierA, qualifierB);

    private static BracketSlotDefinition Bracket(
        string slotId,
        string matchId,
        string round,
        int roundOrder,
        int position,
        string sourceA,
        string sourceB) =>
        new(slotId, matchId, round, roundOrder, position, BracketSlotKind.Knockout, sourceA, sourceB, null, null);
}
