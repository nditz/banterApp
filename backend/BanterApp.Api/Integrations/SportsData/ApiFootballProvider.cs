using System.Net.Http.Headers;
using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// API-Football (api-football.com) provider skeleton.
/// Falls back to <see cref="MockSportsDataProvider"/> when the API key is missing or requests fail.
/// </summary>
public sealed class ApiFootballProvider : ISportsDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly SportsDataOptions _options;
    private readonly MockSportsDataProvider _fallback;
    private readonly ILogger<ApiFootballProvider> _logger;

    public ApiFootballProvider(
        HttpClient httpClient,
        IOptions<SportsDataOptions> options,
        ILogger<ApiFootballProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _fallback = new MockSportsDataProvider();
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        if (!HasApiKey())
        {
            return await _fallback.GetUpcomingFixturesAsync(cancellationToken);
        }

        try
        {
            var url =
                $"{_options.BaseUrl}/fixtures?league={_options.WorldCupLeagueId}" +
                $"&season={_options.WorldCupSeason}&status=NS-TBD";

            var fixtures = await FetchFixturesAsync(url, cancellationToken);
            return fixtures.Count > 0
                ? fixtures
                : await _fallback.GetUpcomingFixturesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football upcoming fixtures request failed; using mock data.");
            return await _fallback.GetUpcomingFixturesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<MatchDto>> GetResultsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!HasApiKey())
        {
            return await _fallback.GetResultsAsync(cancellationToken);
        }

        try
        {
            var url =
                $"{_options.BaseUrl}/fixtures?league={_options.WorldCupLeagueId}" +
                $"&season={_options.WorldCupSeason}&status=FT";

            var fixtures = await FetchFixturesAsync(url, cancellationToken);
            return fixtures.Count > 0
                ? fixtures
                : await _fallback.GetResultsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football results request failed; using mock data.");
            return await _fallback.GetResultsAsync(cancellationToken);
        }
    }

    public async Task<MatchStatisticsDto?> GetMatchStatisticsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        if (!HasApiKey())
        {
            return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }

        try
        {
            var url = $"{_options.BaseUrl}/fixtures/statistics?fixture={matchId}";
            using var request = CreateRequest(url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "API-Football statistics returned {StatusCode} for match {MatchId}.",
                    response.StatusCode,
                    matchId);
                return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
            }

            // TODO Phase 2: map API-Football statistics JSON to MatchStatisticsDto
            _ = await response.Content.ReadAsStringAsync(cancellationToken);
            return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football statistics request failed for match {MatchId}.", matchId);
            return await _fallback.GetMatchStatisticsAsync(matchId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StandingDto>> GetStandingsAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        if (!HasApiKey())
        {
            return await _fallback.GetStandingsAsync(group, cancellationToken);
        }

        try
        {
            var url =
                $"{_options.BaseUrl}/standings?league={_options.WorldCupLeagueId}" +
                $"&season={_options.WorldCupSeason}";

            using var request = CreateRequest(url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API-Football standings returned {StatusCode}.", response.StatusCode);
                return await _fallback.GetStandingsAsync(group, cancellationToken);
            }

            // TODO Phase 2: map API-Football standings JSON, filter by group
            _ = await response.Content.ReadAsStringAsync(cancellationToken);
            return await _fallback.GetStandingsAsync(group, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API-Football standings request failed; using mock data.");
            return await _fallback.GetStandingsAsync(group, cancellationToken);
        }
    }

    private bool HasApiKey() => !string.IsNullOrWhiteSpace(_options.ApiKey);

    private HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-apisports-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<IReadOnlyList<MatchDto>> FetchFixturesAsync(
        string url,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(url);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        // TODO Phase 2: map API-Football fixture response to MatchDto list
        _ = document.RootElement;
        return [];
    }
}
