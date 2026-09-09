using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// API-Football (api-football.com) provider for Premier League fixtures and enrichment data.
/// Falls back to <see cref="MockSportsDataProvider"/> only when the API key is missing.
/// When a key is present, fixture/standings request failures throw
/// <see cref="SportsDataUnavailableException"/> so sync jobs fail visibly.
/// </summary>
public sealed class ApiFootballProvider : ISportsDataProvider, ISportsDataEnrichment
{
    private readonly ApiFootballHttpClient _client;
    private readonly SportsDataOptions _options;
    private readonly MockSportsDataProvider _fallback;
    private readonly ILogger<ApiFootballProvider> _logger;

    public ApiFootballProvider(
        ApiFootballHttpClient client,
        IOptions<SportsDataOptions> options,
        ILogger<ApiFootballProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _fallback = new MockSportsDataProvider();
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchDto>> GetAllFixturesAsync(CancellationToken cancellationToken = default)
    {
        var path =
            $"fixtures?league={_options.LeagueId}&season={_options.Season}";
        // Always apply league id filter when mapping so foreign competitions never enter the PL pipeline.
        return await FetchFixturesAsync(path, _options.LeagueId, allowEmpty: false, _fallback.GetAllFixturesAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(
        CancellationToken cancellationToken = default)
    {
            var path =
                $"fixtures?league={_options.LeagueId}&season={_options.Season}&status=NS";
        return await FetchFixturesAsync(path, _options.LeagueId, allowEmpty: true, _fallback.GetUpcomingFixturesAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetResultsAsync(
        CancellationToken cancellationToken = default)
    {
        var path =
            $"fixtures?league={_options.LeagueId}&season={_options.Season}&status=FT";
        return await FetchFixturesAsync(path, _options.LeagueId, allowEmpty: true, _fallback.GetResultsAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        return await FetchFixturesAsync(
            "fixtures?live=all",
            _options.LeagueId,
            allowEmpty: true,
            _fallback.GetLiveFixturesAsync,
            cancellationToken);
    }

    public async Task<MatchStatisticsDto?> GetMatchStatisticsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        var fixtureId = ExtractFixtureId(matchId);
        if (fixtureId is null || !_client.HasApiKey)
        {
            return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }

        try
        {
            using var document = await _client.GetJsonAsync($"fixtures/statistics?fixture={fixtureId}", cancellationToken);
            return document is null
                ? await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken)
                : ApiFootballFixtureMapper.MapStatistics(document.RootElement, matchId)
                  ?? await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football statistics request failed for {MatchId}; using mock data.", matchId);
            return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StandingDto>> GetStandingsAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAllStandingsAsync(cancellationToken);
        var key = group.Trim().ToUpperInvariant();
        if (all.TryGetValue(key, out var standings) && standings.Count > 0)
        {
            return standings;
        }

        if (_client.HasApiKey)
        {
            throw new SportsDataUnavailableException($"API-Football returned no standings for group {key}.");
        }

        return await _fallback.GetStandingsAsync(group, cancellationToken);
    }

    public async Task<IReadOnlyList<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        if (!_client.HasApiKey)
        {
            return [];
        }

        try
        {
            using var document = await _client.GetJsonAsync(
                $"teams?league={_options.LeagueId}&season={_options.Season}",
                cancellationToken);
            return document is null ? [] : ApiFootballFixtureMapper.MapTeams(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football teams request failed.");
            return [];
        }
    }

    public async Task<TeamSquadDto?> GetTeamSquadAsync(
        string teamProviderId,
        CancellationToken cancellationToken = default)
    {
        if (!_client.HasApiKey)
        {
            return null;
        }

        try
        {
            using var document = await _client.GetJsonAsync(
                $"players/squads?team={teamProviderId}",
                cancellationToken);
            return document is null
                ? null
                : ApiFootballFixtureMapper.MapSquad(document.RootElement, teamProviderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football squad request failed for team {TeamId}.", teamProviderId);
            return null;
        }
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetAllStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_client.HasApiKey)
        {
            _logger.LogWarning("API-Football key missing; using mock standings.");
            return await BuildFallbackStandingsAsync(cancellationToken);
        }

        try
        {
            using var document = await _client.GetJsonAsync(
                $"standings?league={_options.LeagueId}&season={_options.Season}",
                cancellationToken);
            if (document is null)
            {
                throw new SportsDataUnavailableException("API-Football returned no standings document.");
            }

            var mapped = ApiFootballFixtureMapper.MapStandings(document.RootElement);
            if (!mapped.TryGetValue("PL", out var plRows) || plRows.Count == 0)
            {
                throw new SportsDataUnavailableException("API-Football returned no Premier League standings.");
            }

            return mapped;
        }
        catch (SportsDataUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SportsDataUnavailableException("API-Football standings request failed.", ex);
        }
    }

    public async Task<IReadOnlyList<MatchEventDto>> GetMatchEventsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        var fixtureId = ExtractFixtureId(matchId);
        if (fixtureId is null || !_client.HasApiKey)
        {
            return [];
        }

        try
        {
            using var document = await _client.GetJsonAsync($"fixtures/events?fixture={fixtureId}", cancellationToken);
            return document is null ? [] : ApiFootballFixtureMapper.MapEvents(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football events request failed for {MatchId}.", matchId);
            return [];
        }
    }

    public async Task<IReadOnlyList<LineupPlayerDto>> GetMatchLineupsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        var fixtureId = ExtractFixtureId(matchId);
        if (fixtureId is null || !_client.HasApiKey)
        {
            return [];
        }

        try
        {
            using var document = await _client.GetJsonAsync($"fixtures/lineups?fixture={fixtureId}", cancellationToken);
            return document is null ? [] : ApiFootballFixtureMapper.MapLineups(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football lineups request failed for {MatchId}.", matchId);
            return [];
        }
    }

    private async Task<IReadOnlyList<MatchDto>> FetchFixturesAsync(
        string path,
        int? leagueIdFilter,
        bool allowEmpty,
        Func<CancellationToken, Task<IReadOnlyList<MatchDto>>> fallback,
        CancellationToken cancellationToken)
    {
        if (!_client.HasApiKey)
        {
            _logger.LogWarning("API-Football key missing; using mock fixtures for {Path}.", path);
            return await fallback(cancellationToken);
        }

        try
        {
            using var document = await _client.GetJsonAsync(path, cancellationToken);
            if (document is null)
            {
                throw new SportsDataUnavailableException(
                    $"API-Football returned no document for {path}.");
            }

            var fixtures = ApiFootballFixtureMapper.MapFixtures(document.RootElement, leagueIdFilter);
            LogApiErrors(document.RootElement);
            if (fixtures.Count == 0 && !allowEmpty)
            {
                throw new SportsDataUnavailableException(
                    $"API-Football returned 0 fixtures for {path}.");
            }

            return fixtures;
        }
        catch (SportsDataUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SportsDataUnavailableException(
                $"API-Football fixtures request failed for {path}.", ex);
        }
    }

    private void LogApiErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in errors.EnumerateObject())
        {
            _logger.LogWarning(
                "API-Football: {Key} = {Value}",
                property.Name,
                property.Value.ToString());
        }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> BuildFallbackStandingsAsync(
        CancellationToken cancellationToken)
    {
        var groups = new[] { "PL" };
        var result = new Dictionary<string, IReadOnlyList<StandingDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            result[group] = await _fallback.GetStandingsAsync(group, cancellationToken);
        }

        return result;
    }

    private static string? ExtractFixtureId(string matchId)
    {
        const string prefix = "apifb-";
        if (matchId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(matchId[prefix.Length..], out _))
        {
            return matchId[prefix.Length..];
        }

        return null;
    }
}
