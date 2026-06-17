using System.Text.Json;

namespace BanterApp.Api.Services;

/// <summary>
/// FIFA World Cup 2026 Annex C: 495 predefined third-place group combinations mapped to
/// Round of 32 slots (which third-placed group faces each group winner).
/// Source: FIFA 2026 Competition Regulations Annex C (via cup-predictor.com open dataset).
/// </summary>
public static class AnnexCMatrix
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Assignments;
    private static readonly string[] GroupWinnerSlots = ["1A", "1B", "1D", "1E", "1G", "1I", "1K", "1L"];

    static AnnexCMatrix()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "fifa-2026-annex-c-assignments.json");
        if (!File.Exists(path))
        {
            Assignments = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        var map = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in doc.RootElement.GetProperty("assignments").EnumerateObject())
        {
            var slotMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var slot in entry.Value.EnumerateObject())
            {
                slotMap[slot.Name] = slot.Value.GetString() ?? string.Empty;
            }

            map[NormalizeKey(entry.Name)] = slotMap;
        }

        Assignments = map;
    }

    public static IReadOnlyList<string> GroupWinnerSlotKeys => GroupWinnerSlots;

    public static string BuildCombinationKey(IEnumerable<string> qualifyingThirdGroups)
    {
        var letters = qualifyingThirdGroups
            .Select(g => g.Trim().ToUpperInvariant())
            .Where(g => g.Length == 1 && g[0] is >= 'A' and <= 'L')
            .Distinct()
            .OrderBy(g => g, StringComparer.Ordinal)
            .ToList();

        return string.Join(",", letters);
    }

    public static bool TryGetAssignment(string combinationKey, out IReadOnlyDictionary<string, string> slotToThirdGroup)
    {
        slotToThirdGroup = null!;
        if (!Assignments.TryGetValue(NormalizeKey(combinationKey), out var mapping))
        {
            return false;
        }

        slotToThirdGroup = mapping;
        return true;
    }

    public static string? ParseThirdGroupLetter(string annexValue)
    {
        if (string.IsNullOrWhiteSpace(annexValue) || !annexValue.StartsWith('3'))
        {
            return null;
        }

        return annexValue[1..].Trim().ToUpperInvariant();
    }

    private static string NormalizeKey(string key) =>
        string.Join(",", key.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(g => g.Trim().ToUpperInvariant())
            .Distinct()
            .OrderBy(g => g, StringComparer.Ordinal));
}
