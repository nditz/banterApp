using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Secondary provider for validation and filling gaps when API-Football data is missing.
/// </summary>
public interface ISportsDataFallbackProvider
{
    string ProviderName { get; }

    bool IsConfigured { get; }

    Task<IReadOnlyList<MatchDto>> GetFixturesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetStandingsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class SportmonksProvider : ISportsDataFallbackProvider
{
    private readonly HttpClient _httpClient;
    private readonly SportmonksOptions _options;
    private readonly ILogger<SportmonksProvider> _logger;

    public SportmonksProvider(
        HttpClient httpClient,
        IOptions<SportmonksOptions> options,
        ILogger<SportmonksProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "sportmonks";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token);

    public async Task<IReadOnlyList<MatchDto>> GetFixturesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        try
        {
            var path = _options.SeasonId > 0
                ? $"fixtures?filters=fixtureSeasons:{_options.SeasonId}"
                : _options.LeagueId > 0
                    ? $"fixtures?filters=fixtureLeagues:{_options.LeagueId}"
                    : "fixtures";

            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/{path}" +
                $"&api_token={_options.Token}" +
                "&include=participants;scores;league;season;stage;round";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sportmonks fixtures request failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapFixtures(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sportmonks fixtures request failed.");
            return [];
        }
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _options.SeasonId <= 0)
        {
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/standings/seasons/{_options.SeasonId}" +
                $"?api_token={_options.Token}" +
                "&include=participant;group;details";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sportmonks standings request failed: {Status}", (int)response.StatusCode);
                return new Dictionary<string, IReadOnlyList<StandingDto>>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return MapStandings(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sportmonks standings request failed.");
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }
    }

    private static IReadOnlyList<MatchDto> MapFixtures(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var fixtures = new List<MatchDto>();
        foreach (var item in data.EnumerateArray())
        {
            var mapped = MapFixture(item);
            if (mapped is not null)
            {
                fixtures.Add(mapped);
            }
        }

        return fixtures;
    }

    private static MatchDto? MapFixture(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var idEl))
        {
            return null;
        }

        var home = new TeamDto("sm-home", "TBD", "TBD", "TBD");
        var away = new TeamDto("sm-away", "TBD", "TBD", "TBD");
        if (item.TryGetProperty("participants", out var participants) && participants.ValueKind == JsonValueKind.Array)
        {
            var teams = participants.EnumerateArray().Take(2).ToList();
            if (teams.Count > 0)
            {
                home = MapParticipant(teams[0], "sm-home");
            }

            if (teams.Count > 1)
            {
                away = MapParticipant(teams[1], "sm-away");
            }
        }

        var kickoff = item.TryGetProperty("starting_at", out var startEl) &&
                      DateTimeOffset.TryParse(startEl.GetString(), out var parsed)
            ? PostgresUtc.Normalize(parsed)
            : DateTimeOffset.UtcNow;

        int? homeScore = null;
        int? awayScore = null;
        if (item.TryGetProperty("scores", out var scores) && scores.ValueKind == JsonValueKind.Array)
        {
            foreach (var score in scores.EnumerateArray())
            {
                if (!score.TryGetProperty("description", out var desc) ||
                    desc.GetString() != "CURRENT")
                {
                    continue;
                }

                if (score.TryGetProperty("score", out var scoreObj))
                {
                    if (scoreObj.TryGetProperty("participant", out var part) && part.GetString() == "home" &&
                        scoreObj.TryGetProperty("goals", out var goals))
                    {
                        homeScore = goals.GetInt32();
                    }

                    if (scoreObj.TryGetProperty("participant", out var partAway) && partAway.GetString() == "away" &&
                        scoreObj.TryGetProperty("goals", out var awayGoals))
                    {
                        awayScore = awayGoals.GetInt32();
                    }
                }
            }
        }

        return new MatchDto(
            $"sportmonks-{idEl.GetInt32()}",
            home,
            away,
            kickoff,
            "Group Stage",
            string.Empty,
            string.Empty,
            homeScore is not null || awayScore is not null ? "FT" : "NS",
            homeScore,
            awayScore);
    }

    private static TeamDto MapParticipant(JsonElement participant, string fallbackId)
    {
        var id = participant.TryGetProperty("id", out var idEl) ? idEl.GetInt32().ToString() : fallbackId;
        var name = participant.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "TBD" : "TBD";
        var code = participant.TryGetProperty("short_code", out var codeEl) &&
                   !string.IsNullOrWhiteSpace(codeEl.GetString())
            ? codeEl.GetString()!.ToUpperInvariant()
            : name.Length >= 3 ? name[..3].ToUpperInvariant() : "TBD";
        return new TeamDto(id, name, code, code);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<StandingDto>> MapStandings(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }

        var result = new Dictionary<string, List<StandingDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("participant", out var participant))
            {
                continue;
            }

            var groupKey = ResolveGroupKey(item);
            var team = MapParticipant(participant, "sm-team");
            var position = item.TryGetProperty("position", out var posEl) ? posEl.GetInt32() : 0;
            var points = item.TryGetProperty("points", out var ptsEl) ? ptsEl.GetInt32() : 0;
            var (played, won, drawn, lost, goalsFor, goalsAgainst) = MapStandingDetails(item);

            if (!result.TryGetValue(groupKey, out var rows))
            {
                rows = [];
                result[groupKey] = rows;
            }

            rows.Add(new StandingDto(
                position,
                team,
                played,
                won,
                drawn,
                lost,
                goalsFor,
                goalsAgainst,
                goalsFor - goalsAgainst,
                points));
        }

        return result.ToDictionary(
            k => k.Key,
            v => (IReadOnlyList<StandingDto>)v.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static string ResolveGroupKey(JsonElement item)
    {
        if (item.TryGetProperty("group", out var group) &&
            group.TryGetProperty("name", out var groupNameEl))
        {
            var name = groupNameEl.GetString() ?? string.Empty;
            var trimmed = name
                .Replace("Group ", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed.ToUpperInvariant();
            }
        }

        if (item.TryGetProperty("group_id", out var groupIdEl))
        {
            return $"G{groupIdEl.GetInt32()}";
        }

        return "A";
    }

    private static (int Played, int Won, int Drawn, int Lost, int GoalsFor, int GoalsAgainst) MapStandingDetails(
        JsonElement item)
    {
        if (!item.TryGetProperty("details", out var details) || details.ValueKind != JsonValueKind.Array)
        {
            return (0, 0, 0, 0, 0, 0);
        }

        int played = 0, won = 0, drawn = 0, lost = 0, goalsFor = 0, goalsAgainst = 0;
        foreach (var detail in details.EnumerateArray())
        {
            if (!detail.TryGetProperty("type_id", out var typeEl) ||
                !detail.TryGetProperty("value", out var valueEl) ||
                valueEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var value = valueEl.GetInt32();
            switch (typeEl.GetInt32())
            {
                case 129: played = value; break;
                case 130: won = value; break;
                case 131: drawn = value; break;
                case 132: lost = value; break;
                case 133: goalsFor = value; break;
                case 134: goalsAgainst = value; break;
            }
        }

        return (played, won, drawn, lost, goalsFor, goalsAgainst);
    }
}
