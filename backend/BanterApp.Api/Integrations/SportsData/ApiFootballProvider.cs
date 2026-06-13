using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// API-Football (api-football.com) provider for World Cup fixtures and enrichment data.
/// Falls back to <see cref="MockSportsDataProvider"/> when the API key is missing or requests fail.
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
            $"fixtures?league={_options.WorldCupLeagueId}&season={_options.WorldCupSeason}";
        return await FetchFixturesOrFallbackAsync(path, null, _fallback.GetAllFixturesAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        var path =
            $"fixtures?league={_options.WorldCupLeagueId}&season={_options.WorldCupSeason}&status=NS-TBD";
        return await FetchFixturesOrFallbackAsync(path, null, _fallback.GetUpcomingFixturesAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetResultsAsync(
        CancellationToken cancellationToken = default)
    {
        var path =
            $"fixtures?league={_options.WorldCupLeagueId}&season={_options.WorldCupSeason}&status=FT";
        return await FetchFixturesOrFallbackAsync(path, null, _fallback.GetResultsAsync, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        return await FetchFixturesOrFallbackAsync(
            "fixtures?live=all",
            _options.WorldCupLeagueId,
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
        return all.TryGetValue(key, out var standings)
            ? standings
            : await _fallback.GetStandingsAsync(group, cancellationToken);
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
                $"teams?league={_options.WorldCupLeagueId}&season={_options.WorldCupSeason}",
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
            return await BuildFallbackStandingsAsync(cancellationToken);
        }

        try
        {
            using var document = await _client.GetJsonAsync(
                $"standings?league={_options.WorldCupLeagueId}&season={_options.WorldCupSeason}",
                cancellationToken);
            return document is null
                ? await BuildFallbackStandingsAsync(cancellationToken)
                : ApiFootballFixtureMapper.MapStandings(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football standings request failed; using mock data.");
            return await BuildFallbackStandingsAsync(cancellationToken);
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

    private async Task<IReadOnlyList<MatchDto>> FetchFixturesOrFallbackAsync(
        string path,
        int? leagueIdFilter,
        Func<CancellationToken, Task<IReadOnlyList<MatchDto>>> fallback,
        CancellationToken cancellationToken)
    {
        if (!_client.HasApiKey)
        {
            return await fallback(cancellationToken);
        }

        try
        {
            using var document = await _client.GetJsonAsync(path, cancellationToken);
            if (document is null)
            {
                return await fallback(cancellationToken);
            }

            var fixtures = ApiFootballFixtureMapper.MapFixtures(document.RootElement, leagueIdFilter);
            return fixtures.Count > 0 ? fixtures : await fallback(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football fixtures request failed for {Path}; using mock data.", path);
            return await fallback(cancellationToken);
        }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> BuildFallbackStandingsAsync(
        CancellationToken cancellationToken)
    {
        var groups = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
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
