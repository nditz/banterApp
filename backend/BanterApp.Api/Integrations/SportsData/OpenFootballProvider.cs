using System.Text.Json;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// Free World Cup 2026 schedule/results from openfootball/worldcup.json (no API key).
/// Used when paid APIs (API-Football 2026 season) are unavailable.
/// </summary>
public sealed class OpenFootballProvider : ISportsDataFallbackProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenFootballOptions _options;
    private readonly ILogger<OpenFootballProvider> _logger;
    private IReadOnlyList<MatchDto>? _cache;
    private DateTimeOffset _cacheExpires = DateTimeOffset.MinValue;

    public OpenFootballProvider(
        HttpClient httpClient,
        IOptions<OpenFootballOptions> options,
        ILogger<OpenFootballProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "openfootball";

    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.JsonUrl);

    public async Task<IReadOnlyList<MatchDto>> GetFixturesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var all = await LoadFixturesAsync(cancellationToken);
        return all;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<StandingDto>>> GetStandingsAsync(
        CancellationToken cancellationToken = default)
    {
        var fixtures = await LoadFixturesAsync(cancellationToken);
        return OpenFootballMapper.BuildStandings(fixtures);
    }

    private async Task<IReadOnlyList<MatchDto>> LoadFixturesAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null && DateTimeOffset.UtcNow < _cacheExpires)
        {
            return _cache;
        }

        try
        {
            using var response = await _httpClient.GetAsync(_options.JsonUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenFootball fetch failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            _cache = OpenFootballMapper.MapFixtures(document.RootElement);
            _cacheExpires = DateTimeOffset.UtcNow.AddMinutes(15);
            _logger.LogInformation("OpenFootball loaded {Count} fixtures.", _cache.Count);
            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenFootball fetch failed.");
            return [];
        }
    }
}
