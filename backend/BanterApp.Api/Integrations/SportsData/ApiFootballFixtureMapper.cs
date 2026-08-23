using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using BanterApp.Api.Services;

namespace BanterApp.Api.Integrations.SportsData;

public static class ApiFootballFixtureMapper
{
    public static IReadOnlyList<MatchDto> MapFixtures(JsonElement root, int? leagueIdFilter = null)
    {
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var fixtures = new List<MatchDto>();
        foreach (var item in response.EnumerateArray())
        {
            if (leagueIdFilter is not null &&
                item.TryGetProperty("league", out var leagueEl) &&
                leagueEl.TryGetProperty("id", out var leagueIdEl) &&
                leagueIdEl.GetInt32() != leagueIdFilter.Value)
            {
                continue;
            }

            var mapped = MapFixture(item);
            if (mapped is not null)
            {
                fixtures.Add(mapped);
            }
        }

        return fixtures;
    }

    public static MatchStatisticsDto? MapStatistics(JsonElement root, string matchId)
    {
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var teams = response.EnumerateArray().Take(2).ToList();
        if (teams.Count < 2)
        {
            return null;
        }

        var home = ParseTeamStats(teams[0]);
        var away = ParseTeamStats(teams[1]);

        return new MatchStatisticsDto(
            matchId,
            home.GetValueOrDefault("Ball Possession"),
            away.GetValueOrDefault("Ball Possession"),
            home.GetValueOrDefault("Total Shots"),
            away.GetValueOrDefault("Total Shots"),
            home.GetValueOrDefault("Shots on Goal"),
            away.GetValueOrDefault("Shots on Goal"),
            home.GetValueOrDefault("Corner Kicks"),
            away.GetValueOrDefault("Corner Kicks"),
            home.GetValueOrDefault("Fouls"),
            away.GetValueOrDefault("Fouls"),
            home.GetValueOrDefault("Yellow Cards"),
            away.GetValueOrDefault("Yellow Cards"),
            home.GetValueOrDefault("Red Cards"),
            away.GetValueOrDefault("Red Cards"));
    }

    private static Dictionary<string, int> ParseTeamStats(JsonElement teamBlock)
    {
        var stats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!teamBlock.TryGetProperty("statistics", out var statistics) ||
            statistics.ValueKind != JsonValueKind.Array)
        {
            return stats;
        }

        foreach (var stat in statistics.EnumerateArray())
        {
            if (!stat.TryGetProperty("type", out var typeEl) ||
                !stat.TryGetProperty("value", out var valueEl))
            {
                continue;
            }

            stats[typeEl.GetString() ?? string.Empty] = ParseStatValue(valueEl);
        }

        return stats;
    }

    private static int ParseStatValue(JsonElement valueEl)
    {
        if (valueEl.ValueKind == JsonValueKind.Number)
        {
            return valueEl.GetInt32();
        }

        if (valueEl.ValueKind == JsonValueKind.String)
        {
            var raw = valueEl.GetString() ?? "0";
            raw = raw.TrimEnd('%');
            return int.TryParse(raw, out var parsed) ? parsed : 0;
        }

        return 0;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<StandingDto>> MapStandings(JsonElement root)
    {
        var result = new Dictionary<string, List<StandingDto>>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value, StringComparer.OrdinalIgnoreCase);
        }

        foreach (var leagueBlock in response.EnumerateArray())
        {
            if (!leagueBlock.TryGetProperty("league", out var league) ||
                !league.TryGetProperty("standings", out var standings) ||
                standings.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var groupTable in standings.EnumerateArray())
            {
                if (groupTable.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                    var groupKey = "PL";
                    var rows = new List<StandingDto>();
                    var rank = 1;

                    foreach (var row in groupTable.EnumerateArray())
                    {
                        if (row.TryGetProperty("group", out var groupName))
                        {
                            var letter = ExtractGroupLetter(groupName.GetString() ?? string.Empty);
                            groupKey = string.IsNullOrEmpty(letter) ? "PL" : letter;
                        }

                    if (!row.TryGetProperty("team", out var teamEl) ||
                        !row.TryGetProperty("all", out var allEl))
                    {
                        continue;
                    }

                    var team = MapTeam(teamEl);
                    rows.Add(new StandingDto(
                        rank++,
                        team,
                        allEl.GetProperty("played").GetInt32(),
                        allEl.GetProperty("win").GetInt32(),
                        allEl.GetProperty("draw").GetInt32(),
                        allEl.GetProperty("lose").GetInt32(),
                        allEl.GetProperty("goals").GetProperty("for").GetInt32(),
                        allEl.GetProperty("goals").GetProperty("against").GetInt32(),
                        row.GetProperty("goalsDiff").GetInt32(),
                        row.GetProperty("points").GetInt32()));
                }

                if (rows.Count > 0)
                {
                    result[groupKey] = rows;
                }
            }
        }

        return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static MatchDto? MapFixture(JsonElement item)
    {
        if (!item.TryGetProperty("fixture", out var fixture) ||
            !item.TryGetProperty("teams", out var teams) ||
            !item.TryGetProperty("league", out var league))
        {
            return null;
        }

        var home = MapTeam(teams.GetProperty("home"));
        var away = MapTeam(teams.GetProperty("away"));
        var kickoff = fixture.TryGetProperty("date", out var dateEl) &&
                        DateTimeOffset.TryParse(dateEl.GetString(), out var kickoffUtc)
            ? kickoffUtc
            : DateTimeOffset.UtcNow;

        var status = fixture.TryGetProperty("status", out var statusEl) &&
                     statusEl.TryGetProperty("short", out var shortEl)
            ? shortEl.GetString() ?? "NS"
            : "NS";

        var stage = league.TryGetProperty("round", out var roundEl)
            ? roundEl.GetString() ?? "Regular Season"
            : "Regular Season";

        var group = ExtractGroupLetter(stage);
        if (string.IsNullOrEmpty(group))
        {
            group = "PL";
        }
        var venue = fixture.TryGetProperty("venue", out var venueEl) && venueEl.TryGetProperty("name", out var venueName)
            ? venueName.GetString() ?? string.Empty
            : string.Empty;

        int? homeScore = null;
        int? awayScore = null;
        if (item.TryGetProperty("goals", out var goals))
        {
            if (goals.TryGetProperty("home", out var homeGoals) && homeGoals.ValueKind != JsonValueKind.Null)
            {
                homeScore = homeGoals.GetInt32();
            }

            if (goals.TryGetProperty("away", out var awayGoals) && awayGoals.ValueKind != JsonValueKind.Null)
            {
                awayScore = awayGoals.GetInt32();
            }
        }

        var id = fixture.GetProperty("id").GetInt32().ToString();
        return new MatchDto(
            $"apifb-{id}",
            home,
            away,
            kickoff,
            stage,
            group,
            venue,
            status,
            homeScore,
            awayScore,
            MatchweekParser.TryParse(stage));
    }

    private static TeamDto MapTeam(JsonElement teamEl)
    {
        var id = teamEl.TryGetProperty("id", out var idEl) ? idEl.GetInt32().ToString() : "team";
        var name = teamEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "TBD" : "TBD";
        var code = teamEl.TryGetProperty("code", out var codeEl) && !string.IsNullOrWhiteSpace(codeEl.GetString())
            ? codeEl.GetString()!.ToUpperInvariant()
            : name.Length >= 3
                ? name[..3].ToUpperInvariant()
                : "TBD";
        var logo = teamEl.TryGetProperty("logo", out var logoEl) ? logoEl.GetString() : null;

        return new TeamDto(id, name, code, code, logo);
    }

    private static string ExtractGroupLetter(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var match = System.Text.RegularExpressions.Regex.Match(value, @"Group\s+([A-L])", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Groups[1].Value.ToUpperInvariant();
        }

        return value.Contains("Final", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("Round", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : string.Empty;
    }

    public static IReadOnlyList<TeamDto> MapTeams(JsonElement root)
    {
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var teams = new List<TeamDto>();
        foreach (var item in response.EnumerateArray())
        {
            if (!item.TryGetProperty("team", out var teamEl))
            {
                continue;
            }

            teams.Add(MapTeam(teamEl));
        }

        return teams;
    }

    public static TeamSquadDto? MapSquad(JsonElement root, string teamProviderId)
    {
        if (!root.TryGetProperty("response", out var response) ||
            response.ValueKind != JsonValueKind.Array ||
            response.GetArrayLength() == 0)
        {
            return null;
        }

        var block = response[0];
        if (!block.TryGetProperty("team", out var teamEl) ||
            !block.TryGetProperty("players", out var playersEl) ||
            playersEl.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var team = MapTeam(teamEl);
        var players = new List<SquadPlayerDto>();
        foreach (var playerBlock in playersEl.EnumerateArray())
        {
            if (!playerBlock.TryGetProperty("id", out var idEl) ||
                !playerBlock.TryGetProperty("name", out var nameEl))
            {
                continue;
            }

            int? number = null;
            if (playerBlock.TryGetProperty("number", out var numEl) && numEl.ValueKind == JsonValueKind.Number)
            {
                number = numEl.GetInt32();
            }

            string? position = null;
            if (playerBlock.TryGetProperty("position", out var posEl))
            {
                position = posEl.GetString();
            }

            players.Add(new SquadPlayerDto(
                idEl.GetInt32().ToString(),
                nameEl.GetString() ?? "Unknown",
                number,
                position));
        }

        return new TeamSquadDto(teamProviderId, team.Name, team.Code, players);
    }

    public static IReadOnlyList<MatchEventDto> MapEvents(JsonElement root)
    {
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var events = new List<MatchEventDto>();
        foreach (var item in response.EnumerateArray())
        {
            var time = item.TryGetProperty("time", out var timeEl) && timeEl.TryGetProperty("elapsed", out var elapsed)
                ? elapsed.GetInt32()
                : 0;
            var type = item.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "Event" : "Event";
            var detail = item.TryGetProperty("detail", out var detailEl) ? detailEl.GetString() : null;
            var player = item.TryGetProperty("player", out var playerEl) && playerEl.TryGetProperty("name", out var playerName)
                ? playerName.GetString()
                : null;
            string? teamCode = null;
            if (item.TryGetProperty("team", out var teamEl))
            {
                var team = MapTeam(teamEl);
                teamCode = team.Code;
            }

            var providerId = item.TryGetProperty("id", out var idEl)
                ? idEl.GetInt32().ToString()
                : $"{type}-{time}-{player}";

            events.Add(new MatchEventDto(providerId, time, type, teamCode, player, detail));
        }

        return events;
    }

    public static IReadOnlyList<LineupPlayerDto> MapLineups(JsonElement root)
    {
        if (!root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var players = new List<LineupPlayerDto>();
        foreach (var teamBlock in response.EnumerateArray())
        {
            if (!teamBlock.TryGetProperty("team", out var teamEl))
            {
                continue;
            }

            var team = MapTeam(teamEl);
            if (teamBlock.TryGetProperty("startXI", out var startXi) && startXi.ValueKind == JsonValueKind.Array)
            {
                foreach (var slot in startXi.EnumerateArray())
                {
                    if (!slot.TryGetProperty("player", out var playerEl))
                    {
                        continue;
                    }

                    players.Add(MapLineupPlayer(playerEl, team.Code, false));
                }
            }

            if (teamBlock.TryGetProperty("substitutes", out var subs) && subs.ValueKind == JsonValueKind.Array)
            {
                foreach (var slot in subs.EnumerateArray())
                {
                    if (!slot.TryGetProperty("player", out var playerEl))
                    {
                        continue;
                    }

                    players.Add(MapLineupPlayer(playerEl, team.Code, true));
                }
            }
        }

        return players;
    }

    private static LineupPlayerDto MapLineupPlayer(JsonElement playerEl, string teamCode, bool isSubstitute)
    {
        var name = playerEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown";
        int? number = null;
        if (playerEl.TryGetProperty("number", out var numEl) && numEl.ValueKind == JsonValueKind.Number)
        {
            number = numEl.GetInt32();
        }

        var position = playerEl.TryGetProperty("pos", out var posEl) ? posEl.GetString() ?? string.Empty : string.Empty;
        return new LineupPlayerDto(teamCode, number, name, position, isSubstitute);
    }
}
