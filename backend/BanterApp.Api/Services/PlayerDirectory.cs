using System.Text.Json;

namespace BanterApp.Api.Services;

/// <summary>
/// In-memory directory of national-team players grouped by country (team code), loaded from
/// <c>Data/tournament-squads.json</c>. Powers the tournament bonus pick search/autocomplete.
/// It is merged at query time with any live lineup data so real squad names flow through once synced.
/// </summary>
public sealed class PlayerDirectory
{
    private readonly IReadOnlyList<DirectoryPlayer> _players;
    private readonly IReadOnlyDictionary<string, string> _teamNames;

    public PlayerDirectory()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "tournament-squads.json");
        var players = new List<DirectoryPlayer>();
        var teamNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);

            if (doc.RootElement.TryGetProperty("teams", out var teams))
            {
                foreach (var team in teams.EnumerateArray())
                {
                    var code = team.TryGetProperty("code", out var codeEl) ? codeEl.GetString() ?? "" : "";
                    var name = team.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? code : code;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        continue;
                    }

                    var normalizedCode = code.Trim().ToUpperInvariant();
                    teamNames[normalizedCode] = name;

                    if (!team.TryGetProperty("players", out var playerList))
                    {
                        continue;
                    }

                    foreach (var player in playerList.EnumerateArray())
                    {
                        var playerName = player.GetString();
                        if (!string.IsNullOrWhiteSpace(playerName))
                        {
                            players.Add(new DirectoryPlayer(playerName.Trim(), normalizedCode, name));
                        }
                    }
                }
            }
        }

        _players = players;
        _teamNames = teamNames;
    }

    public string? GetTeamName(string teamCode) =>
        _teamNames.TryGetValue(teamCode.Trim().ToUpperInvariant(), out var name) ? name : null;

    /// <summary>All directory players (already deduplicated per country in the source data).</summary>
    public IReadOnlyList<DirectoryPlayer> All => _players;

    /// <summary>
    /// Searches the directory by player name (case-insensitive), optionally scoped to a country.
    /// Prefix matches rank ahead of substring matches, then alphabetical.
    /// </summary>
    public IReadOnlyList<DirectoryPlayer> Search(string? query, string? teamCode, int limit)
    {
        IEnumerable<DirectoryPlayer> source = _players;

        if (!string.IsNullOrWhiteSpace(teamCode))
        {
            var code = teamCode.Trim().ToUpperInvariant();
            source = source.Where(p => p.TeamCode == code);
        }

        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            // Browse mode: group by country so the initial list reads as a per-nation roster.
            return source
                .OrderBy(p => p.TeamName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.PlayerName, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
        }

        return source
            .Select(p => new
            {
                Player = p,
                Rank = MatchRank(p.PlayerName, trimmed)
            })
            .Where(x => x.Rank >= 0)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Player.PlayerName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Player)
            .ToList();
    }

    /// <summary>-1 = no match, 0 = prefix match, 1 = word-start match, 2 = substring match.</summary>
    private static int MatchRank(string name, string query)
    {
        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => w.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 1;
        }

        return name.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 : -1;
    }
}

public sealed record DirectoryPlayer(string PlayerName, string TeamCode, string TeamName);
