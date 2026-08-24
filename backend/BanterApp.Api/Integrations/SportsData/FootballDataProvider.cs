using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

public sealed class FootballDataProvider : ISportsDataFallbackProvider
{
    private readonly HttpClient _httpClient;
    private readonly FootballDataOptions _options;
    private readonly ILogger<FootballDataProvider> _logger;

    public FootballDataProvider(
        HttpClient httpClient,
        IOptions<FootballDataOptions> options,
        ILogger<FootballDataProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "football_data";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token);

    public async Task<IReadOnlyList<MatchDto>> GetFixturesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/competitions/{_options.CompetitionCode}/matches";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", _options.Token);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("football-data.org fixtures request failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapMatches(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "football-data.org fixtures request failed.");
            return [];
        }
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/competitions/{_options.CompetitionCode}/standings";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Auth-Token", _options.Token);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<string, IReadOnlyList<StandingDto>>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapStandings(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "football-data.org standings request failed.");
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }
    }

    private static IReadOnlyList<MatchDto> MapMatches(JsonElement root)
    {
        if (!root.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var fixtures = new List<MatchDto>();
        foreach (var match in matches.EnumerateArray())
        {
            if (!match.TryGetProperty("id", out var idEl))
            {
                continue;
            }

            var home = MapTeam(match.GetProperty("homeTeam"));
            var away = MapTeam(match.GetProperty("awayTeam"));
            var kickoff = match.TryGetProperty("utcDate", out var dateEl) &&
                          DateTimeOffset.TryParse(dateEl.GetString(), out var parsed)
                ? parsed
                : DateTimeOffset.UtcNow;
            var status = match.TryGetProperty("status", out var statusEl) ? statusEl.GetString() ?? "NS" : "NS";
            int? homeScore = match.TryGetProperty("score", out var score) &&
                             score.TryGetProperty("fullTime", out var ft) &&
                             ft.TryGetProperty("home", out var homeGoals) &&
                             homeGoals.ValueKind == JsonValueKind.Number
                ? homeGoals.GetInt32()
                : null;
            int? awayScore = match.TryGetProperty("score", out var score2) &&
                             score2.TryGetProperty("fullTime", out var ft2) &&
                             ft2.TryGetProperty("away", out var awayGoals) &&
                             awayGoals.ValueKind == JsonValueKind.Number
                ? awayGoals.GetInt32()
                : null;

            fixtures.Add(new MatchDto(
                $"fd-{idEl.GetInt32()}",
                home,
                away,
                kickoff,
                match.TryGetProperty("stage", out var stageEl) ? stageEl.GetString() ?? "Group" : "Group",
                string.Empty,
                match.TryGetProperty("venue", out var venueEl) ? venueEl.GetString() ?? string.Empty : string.Empty,
                status == "FINISHED" ? "FT" : status,
                homeScore,
                awayScore));
        }

        return fixtures;
    }

    private static TeamDto MapTeam(JsonElement teamEl)
    {
        var id = teamEl.TryGetProperty("id", out var idEl) ? idEl.GetInt32().ToString() : "fd-team";
        var name = teamEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "TBD" : "TBD";
        var tla = teamEl.TryGetProperty("tla", out var tlaEl) && !string.IsNullOrWhiteSpace(tlaEl.GetString())
            ? tlaEl.GetString()!.ToUpperInvariant()
            : name.Length >= 3 ? name[..3].ToUpperInvariant() : "TBD";
        var crest = teamEl.TryGetProperty("crest", out var crestEl) ? crestEl.GetString() : null;
        return new TeamDto(id, name, tla, tla, crest);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<StandingDto>> MapStandings(JsonElement root)
    {
        var result = new Dictionary<string, List<StandingDto>>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("standings", out var standings) || standings.ValueKind != JsonValueKind.Array)
        {
            return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value);
        }

        foreach (var table in standings.EnumerateArray())
        {
            if (!table.TryGetProperty("group", out var groupEl) ||
                !table.TryGetProperty("table", out var rows) ||
                rows.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var groupKey = groupEl.GetString()?.Replace("GROUP_", "", StringComparison.OrdinalIgnoreCase) ?? "A";
            var list = new List<StandingDto>();
            foreach (var row in rows.EnumerateArray())
            {
                var team = MapTeam(row.GetProperty("team"));
                list.Add(new StandingDto(
                    row.GetProperty("position").GetInt32(),
                    team,
                    row.GetProperty("playedGames").GetInt32(),
                    row.GetProperty("won").GetInt32(),
                    row.GetProperty("draw").GetInt32(),
                    row.GetProperty("lost").GetInt32(),
                    row.GetProperty("goalsFor").GetInt32(),
                    row.GetProperty("goalsAgainst").GetInt32(),
                    row.GetProperty("goalDifference").GetInt32(),
                    row.GetProperty("points").GetInt32()));
            }

            if (list.Count > 0)
            {
                result[groupKey] = list;
            }
        }

        return result.ToDictionary(k => k.Key, v => (IReadOnlyList<StandingDto>)v.Value, StringComparer.OrdinalIgnoreCase);
    }
}
