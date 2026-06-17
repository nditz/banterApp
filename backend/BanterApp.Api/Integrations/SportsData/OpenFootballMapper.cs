using System.Text.Json;
using System.Text.RegularExpressions;
using BanterApp.Api.Integrations.SportsData.Dtos;

namespace BanterApp.Api.Integrations.SportsData;

public static partial class OpenFootballMapper
{
    public static IReadOnlyList<MatchDto> MapFixtures(JsonElement root)
    {
        if (!root.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var groupCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var results = new List<MatchDto>();

        foreach (var item in matches.EnumerateArray())
        {
            var mapped = MapMatch(item, groupCounters);
            if (mapped is not null)
            {
                results.Add(mapped);
            }
        }

        return results;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<StandingDto>> BuildStandings(
        IReadOnlyList<MatchDto> fixtures)
    {
        var tables = new Dictionary<string, Dictionary<string, StandingAccumulator>>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in fixtures.Where(m => !string.IsNullOrWhiteSpace(m.Group) && m.Status == "FT"))
        {
            if (match.HomeScore is null || match.AwayScore is null)
            {
                continue;
            }

            var group = match.Group.Trim().ToUpperInvariant();
            if (!tables.TryGetValue(group, out var teams))
            {
                teams = new Dictionary<string, StandingAccumulator>(StringComparer.OrdinalIgnoreCase);
                tables[group] = teams;
            }

            EnsureTeam(teams, match.HomeTeam);
            EnsureTeam(teams, match.AwayTeam);

            var home = teams[match.HomeTeam.Code];
            var away = teams[match.AwayTeam.Code];
            home.Played++;
            away.Played++;
            home.GoalsFor += match.HomeScore.Value;
            home.GoalsAgainst += match.AwayScore.Value;
            away.GoalsFor += match.AwayScore.Value;
            away.GoalsAgainst += match.HomeScore.Value;

            if (match.HomeScore > match.AwayScore)
            {
                home.Won++;
                away.Lost++;
                home.Points += 3;
            }
            else if (match.HomeScore < match.AwayScore)
            {
                away.Won++;
                home.Lost++;
                away.Points += 3;
            }
            else
            {
                home.Drawn++;
                away.Drawn++;
                home.Points++;
                away.Points++;
            }
        }

        return tables.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<StandingDto>)kvp.Value.Values
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .ThenByDescending(t => t.GoalsFor)
                .Select((t, index) => new StandingDto(
                    index + 1,
                    t.Team,
                    t.Played,
                    t.Won,
                    t.Drawn,
                    t.Lost,
                    t.GoalsFor,
                    t.GoalsAgainst,
                    t.GoalDifference,
                    t.Points))
                .ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureTeam(Dictionary<string, StandingAccumulator> teams, TeamDto team)
    {
        if (!teams.ContainsKey(team.Code))
        {
            teams[team.Code] = new StandingAccumulator(team);
        }
    }

    private static MatchDto? MapMatch(JsonElement item, Dictionary<string, int> groupCounters)
    {
        var team1 = item.GetProperty("team1").GetString()?.Trim();
        var team2 = item.GetProperty("team2").GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(team1) || string.IsNullOrWhiteSpace(team2))
        {
            return null;
        }

        var round = item.TryGetProperty("round", out var roundEl) ? roundEl.GetString() ?? string.Empty : string.Empty;
        var group = ExtractGroup(item);
        var stage = round;
        var venue = item.TryGetProperty("ground", out var groundEl) ? groundEl.GetString() ?? string.Empty : string.Empty;

        var kickoff = ParseKickoff(item);
        var (homeScore, awayScore, status) = ParseScore(item, kickoff);

        var id = BuildId(item, group, groupCounters, team1, team2, kickoff);
        var home = ToTeam(team1);
        var away = ToTeam(team2);

        return new MatchDto(
            id,
            home,
            away,
            kickoff,
            stage,
            group,
            venue,
            status,
            homeScore,
            awayScore);
    }

    private static string BuildId(
        JsonElement item,
        string group,
        Dictionary<string, int> groupCounters,
        string team1,
        string team2,
        DateTimeOffset kickoff)
    {
        if (item.TryGetProperty("num", out var numEl) && numEl.TryGetInt32(out var num))
        {
            return $"of26-ko-{num}";
        }

        if (!string.IsNullOrWhiteSpace(group))
        {
            var key = group.ToUpperInvariant();
            groupCounters.TryGetValue(key, out var count);
            count++;
            groupCounters[key] = count;
            return $"of26-grp-{key}-{count}";
        }

        return $"of26-{Slug(team1)}-{Slug(team2)}-{kickoff:yyyyMMdd}";
    }

    private static string ExtractGroup(JsonElement item)
    {
        if (item.TryGetProperty("group", out var groupEl))
        {
            var raw = groupEl.GetString() ?? string.Empty;
            var match = GroupLetter().Match(raw);
            if (match.Success)
            {
                return match.Groups[1].Value.ToUpperInvariant();
            }
        }

        return string.Empty;
    }

    private static (int? Home, int? Away, string Status) ParseScore(
        JsonElement item,
        DateTimeOffset kickoff)
    {
        if (!item.TryGetProperty("score", out var scoreEl) || scoreEl.ValueKind != JsonValueKind.Object)
        {
            return (null, null, kickoff <= DateTimeOffset.UtcNow ? "NS" : "NS");
        }

        if (!scoreEl.TryGetProperty("ft", out var ftEl) || ftEl.ValueKind != JsonValueKind.Array)
        {
            return (null, null, "NS");
        }

        var values = ftEl.EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Number ? v.GetInt32() : ParseInt(v.GetString()))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (values.Count >= 2)
        {
            return (values[0], values[1], "FT");
        }

        if (values.Count == 1 && kickoff <= DateTimeOffset.UtcNow.AddHours(-2))
        {
            return (values[0], 0, "FT");
        }

        return (null, null, kickoff <= DateTimeOffset.UtcNow ? "LIVE" : "NS");
    }

    private static int? ParseInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), out var value) ? value : null;
    }

    private static DateTimeOffset ParseKickoff(JsonElement item)
    {
        var date = item.GetProperty("date").GetString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var time = item.TryGetProperty("time", out var timeEl) ? timeEl.GetString() ?? "12:00 UTC" : "12:00 UTC";

        var offsetMatch = UtcOffset().Match(time);
        var offsetHours = offsetMatch.Success && int.TryParse(offsetMatch.Groups[1].Value, out var h) ? h : 0;
        var clock = time.Split(' ')[0];

        if (!DateTime.TryParse($"{date} {clock}", out var local))
        {
            return DateTimeOffset.UtcNow;
        }

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TimeSpan.FromHours(offsetHours)).ToUniversalTime();
    }

    private static TeamDto ToTeam(string name)
    {
        var code = ResolveFifaCode(name);
        return new TeamDto(Slug(name), name, code, code);
    }

    private static string ResolveFifaCode(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "TBD";
        }

        if (PlaceholderTeam().IsMatch(trimmed))
        {
            return "TBD";
        }

        if (TeamNameToFifa.TryGetValue(trimmed, out var code))
        {
            return code;
        }

        return trimmed.Length >= 3
            ? trimmed[..3].ToUpperInvariant()
            : trimmed.ToUpperInvariant();
    }

    private static readonly Dictionary<string, string> TeamNameToFifa =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Algeria"] = "ALG",
            ["Argentina"] = "ARG",
            ["Australia"] = "AUS",
            ["Austria"] = "AUT",
            ["Belgium"] = "BEL",
            ["Bosnia & Herzegovina"] = "BIH",
            ["Bosnia and Herzegovina"] = "BIH",
            ["Brazil"] = "BRA",
            ["Canada"] = "CAN",
            ["Cape Verde"] = "CPV",
            ["Colombia"] = "COL",
            ["Croatia"] = "CRO",
            ["Curaçao"] = "CUW",
            ["Curacao"] = "CUW",
            ["Czech Republic"] = "CZE",
            ["DR Congo"] = "COD",
            ["Ecuador"] = "ECU",
            ["Egypt"] = "EGY",
            ["England"] = "ENG",
            ["France"] = "FRA",
            ["Germany"] = "GER",
            ["Ghana"] = "GHA",
            ["Haiti"] = "HAI",
            ["Iran"] = "IRN",
            ["Iraq"] = "IRQ",
            ["Ivory Coast"] = "CIV",
            ["Côte d'Ivoire"] = "CIV",
            ["Japan"] = "JPN",
            ["Jordan"] = "JOR",
            ["Mexico"] = "MEX",
            ["Morocco"] = "MAR",
            ["Netherlands"] = "NED",
            ["New Zealand"] = "NZL",
            ["Norway"] = "NOR",
            ["Panama"] = "PAN",
            ["Paraguay"] = "PAR",
            ["Portugal"] = "POR",
            ["Qatar"] = "QAT",
            ["Saudi Arabia"] = "KSA",
            ["Scotland"] = "SCO",
            ["Senegal"] = "SEN",
            ["South Africa"] = "RSA",
            ["South Korea"] = "KOR",
            ["Spain"] = "ESP",
            ["Sweden"] = "SWE",
            ["Switzerland"] = "SUI",
            ["Tunisia"] = "TUN",
            ["Turkey"] = "TUR",
            ["Türkiye"] = "TUR",
            ["USA"] = "USA",
            ["United States"] = "USA",
            ["Uruguay"] = "URU",
            ["Uzbekistan"] = "UZB",
        };

    private static string Slug(string value) =>
        NonAlphaNumeric().Replace(value.ToLowerInvariant(), "-").Trim('-');

    private sealed class StandingAccumulator(TeamDto team)
    {
        public TeamDto Team { get; } = team;
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points { get; set; }
    }

    [GeneratedRegex(@"^(?:[12][A-L]|W\d+|L\d+|3[A-L](?:/[A-L])*)$", RegexOptions.IgnoreCase)]
    private static partial Regex PlaceholderTeam();

    [GeneratedRegex(@"Group\s+([A-L])", RegexOptions.IgnoreCase)]
    private static partial Regex GroupLetter();

    [GeneratedRegex(@"UTC([+-]?\d+)")]
    private static partial Regex UtcOffset();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphaNumeric();
}
