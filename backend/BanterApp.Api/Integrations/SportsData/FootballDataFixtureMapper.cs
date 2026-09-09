using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;

namespace BanterApp.Api.Integrations.SportsData;

public static class FootballDataFixtureMapper
{
    public static IReadOnlyList<MatchDto> MapMatches(JsonElement root)
    {
        if (!root.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var fixtures = new List<MatchDto>();
        foreach (var match in matches.EnumerateArray())
        {
            var mapped = MapMatch(match);
            if (mapped is not null)
            {
                fixtures.Add(mapped);
            }
        }

        return fixtures;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<StandingDto>> MapStandings(JsonElement root)
    {
        var result = new Dictionary<string, List<StandingDto>>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("standings", out var standings) || standings.ValueKind != JsonValueKind.Array)
        {
            return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value);
        }

        foreach (var table in standings.EnumerateArray())
        {
            if (!table.TryGetProperty("table", out var rows) || rows.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var type = table.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (!string.IsNullOrWhiteSpace(type) &&
                !string.Equals(type, "TOTAL", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var groupKey = ReadGroupKey(table);
            var list = new List<StandingDto>();
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("team", out var teamEl))
                {
                    continue;
                }

                var team = MapTeam(teamEl);
                list.Add(new StandingDto(
                    ReadInt(row, "position") ?? 0,
                    team,
                    ReadInt(row, "playedGames") ?? 0,
                    ReadInt(row, "won") ?? 0,
                    ReadInt(row, "draw") ?? 0,
                    ReadInt(row, "lost") ?? 0,
                    ReadInt(row, "goalsFor") ?? 0,
                    ReadInt(row, "goalsAgainst") ?? 0,
                    ReadInt(row, "goalDifference") ?? 0,
                    ReadInt(row, "points") ?? 0));
            }

            if (list.Count > 0)
            {
                result[groupKey] = list;
            }
        }

        return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static MatchDto? MapMatch(JsonElement match)
    {
        if (!match.TryGetProperty("id", out var idEl) ||
            !match.TryGetProperty("homeTeam", out var homeEl) ||
            !match.TryGetProperty("awayTeam", out var awayEl))
        {
            return null;
        }

        var id = ReadId(idEl);
        if (id is null)
        {
            return null;
        }

        var matchweek = ReadInt(match, "matchday");
        var stageRaw = match.TryGetProperty("stage", out var stageEl) ? stageEl.GetString() : null;
        var stage = matchweek is > 0
            ? $"Regular Season - {matchweek}"
            : string.IsNullOrWhiteSpace(stageRaw) ||
              string.Equals(stageRaw, "REGULAR_SEASON", StringComparison.OrdinalIgnoreCase)
                ? "Regular Season"
                : stageRaw;

        var kickoff = match.TryGetProperty("utcDate", out var dateEl) &&
                      DateTimeOffset.TryParse(dateEl.GetString(), out var parsed)
            ? PostgresUtc.Normalize(parsed)
            : DateTimeOffset.UtcNow;

        int? homeScore = null;
        int? awayScore = null;
        if (match.TryGetProperty("score", out var score) &&
            score.TryGetProperty("fullTime", out var ft))
        {
            homeScore = ReadInt(ft, "home");
            awayScore = ReadInt(ft, "away");
        }

        var venue = match.TryGetProperty("venue", out var venueEl) && venueEl.ValueKind == JsonValueKind.String
            ? venueEl.GetString() ?? string.Empty
            : string.Empty;

        return new MatchDto(
            $"fd-{id}",
            MapTeam(homeEl),
            MapTeam(awayEl),
            kickoff,
            stage,
            "PL",
            venue,
            MapStatus(match.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null),
            homeScore,
            awayScore,
            matchweek is >= 1 and <= 38 ? matchweek : MatchweekParser.TryParse(stage));
    }

    private static TeamDto MapTeam(JsonElement teamEl)
    {
        var id = teamEl.TryGetProperty("id", out var idEl) ? ReadId(idEl) ?? "fd-team" : "fd-team";
        var name = teamEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "TBD" : "TBD";
        var tla = teamEl.TryGetProperty("tla", out var tlaEl) && !string.IsNullOrWhiteSpace(tlaEl.GetString())
            ? tlaEl.GetString()!.ToUpperInvariant()
            : name.Length >= 3 ? name[..3].ToUpperInvariant() : "TBD";
        var crest = teamEl.TryGetProperty("crest", out var crestEl) ? crestEl.GetString() : null;
        return new TeamDto(id, name, tla, tla, crest);
    }

    private static string ReadGroupKey(JsonElement table)
    {
        if (table.TryGetProperty("group", out var groupEl) &&
            groupEl.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(groupEl.GetString()))
        {
            return groupEl.GetString()!.Replace("GROUP_", "", StringComparison.OrdinalIgnoreCase);
        }

        return "PL";
    }

    public static string MapStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "FINISHED" => "FT",
        "IN_PLAY" or "LIVE" or "PAUSED" => "LIVE",
        "TIMED" or "SCHEDULED" => "NS",
        "POSTPONED" => "PST",
        "CANCELLED" or "CANCELED" => "CANC",
        "SUSPENDED" => "SUSP",
        _ => string.IsNullOrWhiteSpace(status) ? "NS" : status
    };

    private static string? ReadId(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetInt64(out var n) ? n.ToString() : el.GetRawText(),
            JsonValueKind.String => el.GetString(),
            _ => null
        };

    private static int? ReadInt(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value) ? value : null;
    }
}
