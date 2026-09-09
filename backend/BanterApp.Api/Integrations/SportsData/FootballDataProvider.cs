using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

public sealed class FootballDataProvider : ISportsDataProvider, ISportsDataFallbackProvider
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

    public Task<IReadOnlyList<MatchDto>> GetAllFixturesAsync(CancellationToken cancellationToken = default) =>
        GetFixturesAsync(cancellationToken);

    public async Task<IReadOnlyList<MatchDto>> GetUpcomingFixturesAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-3);
        var fixtures = await GetFixturesAsync(cancellationToken);
        return fixtures
            .Where(m =>
                (string.Equals(m.Status, "NS", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(m.Status, "PST", StringComparison.OrdinalIgnoreCase)) &&
                m.KickoffUtc >= cutoff)
            .ToList();
    }

    public async Task<IReadOnlyList<MatchDto>> GetResultsAsync(CancellationToken cancellationToken = default)
    {
        var fixtures = await GetFixturesAsync(cancellationToken);
        return fixtures
            .Where(m =>
                string.Equals(m.Status, "FT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Status, "AET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Status, "PEN", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<MatchDto>> GetLiveFixturesAsync(CancellationToken cancellationToken = default)
    {
        var fixtures = await GetFixturesAsync(cancellationToken);
        return fixtures
            .Where(m => string.Equals(m.Status, "LIVE", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Task<MatchStatisticsDto?> GetMatchStatisticsAsync(
        string matchId,
        CancellationToken cancellationToken = default)
    {
        _ = matchId;
        _ = cancellationToken;
        return Task.FromResult<MatchStatisticsDto?>(null);
    }

    public async Task<IReadOnlyList<StandingDto>> GetStandingsAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var tables = await GetStandingsAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(group) && tables.TryGetValue(group, out var rows))
        {
            return rows;
        }

        if (tables.TryGetValue("PL", out var premierLeague))
        {
            return premierLeague;
        }

        return tables.Values.FirstOrDefault() ?? [];
    }

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
            var fixtures = FootballDataFixtureMapper.MapMatches(document.RootElement);
            _logger.LogInformation(
                "football-data.org returned {Count} Premier League fixtures.",
                fixtures.Count);
            return fixtures;
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
            return FootballDataFixtureMapper.MapStandings(document.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "football-data.org standings request failed.");
            return new Dictionary<string, IReadOnlyList<StandingDto>>();
        }
    }
}
