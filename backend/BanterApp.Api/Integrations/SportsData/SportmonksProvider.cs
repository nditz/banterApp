using System.Text.Json;
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
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/fixtures?api_token={_options.Token}" +
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

    public Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: wire season-specific standings when WorldCupLeagueId/season_id is configured.
        _logger.LogDebug("Sportmonks standings not configured for season_id; returning empty.");
        return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>>(
            new Dictionary<string, IReadOnlyList<StandingDto>>());
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
            ? parsed
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
        var code = name.Length >= 3 ? name[..3].ToUpperInvariant() : "TBD";
        return new TeamDto(id, name, code, code);
    }
}
