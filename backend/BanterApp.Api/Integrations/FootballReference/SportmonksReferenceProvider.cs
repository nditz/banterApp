using System.Text.Json;
using BanterApp.Api.Integrations.FootballReference.Dtos;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.FootballReference;

/// <summary>
/// Sportmonks adapter for football reference data (squads, top scorers/assists).
/// </summary>
public sealed class SportmonksReferenceProvider : IFootballReferenceDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly SportmonksOptions _options;
    private readonly FootballReferenceDataOptions _refOptions;
    private readonly ILogger<SportmonksReferenceProvider> _logger;

    public SportmonksReferenceProvider(
        HttpClient httpClient,
        IOptions<SportmonksOptions> options,
        IOptions<FootballReferenceDataOptions> refOptions,
        ILogger<SportmonksReferenceProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _refOptions = refOptions.Value;
        _logger = logger;
    }

    public string ProviderName => "sportmonks";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token);

    public async Task<IReadOnlyList<CountryDto>> SyncCountriesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/core/countries" +
                $"?api_token={_options.Token}";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sportmonks countries request failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<CountryDto>();
            foreach (var item in data.EnumerateArray())
            {
                var externalId = item.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
                var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var code = item.TryGetProperty("iso2", out var codeEl) ? codeEl.GetString() : null;
                var flag = item.TryGetProperty("image_path", out var flagEl) ? flagEl.GetString() : null;
                var continent = item.TryGetProperty("continent", out var contEl) ? contEl.GetString() : null;

                results.Add(new CountryDto(externalId, name, code, flag, continent, null, item.GetRawText()));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sportmonks countries sync failed.");
            return [];
        }
    }

    public async Task<IReadOnlyList<PlayerDto>> SyncPlayersAsync(
        SyncPlayersParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _options.SeasonId <= 0)
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/squads/seasons/{_options.SeasonId}" +
                $"?api_token={_options.Token}&include=player;team";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sportmonks squads request failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<PlayerDto>();
            foreach (var squad in data.EnumerateArray())
            {
                if (!squad.TryGetProperty("player", out var playerEl))
                {
                    continue;
                }

                var externalId = playerEl.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
                var displayName = playerEl.TryGetProperty("display_name", out var dnEl)
                    ? dnEl.GetString()
                    : playerEl.TryGetProperty("common_name", out var cnEl)
                        ? cnEl.GetString()
                        : null;
                if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var teamExternalId = squad.TryGetProperty("team", out var teamEl) &&
                                     teamEl.TryGetProperty("id", out var teamIdEl)
                    ? teamIdEl.GetRawText()
                    : null;
                var teamName = teamEl.TryGetProperty("name", out var teamNameEl)
                    ? teamNameEl.GetString()
                    : null;
                var photo = playerEl.TryGetProperty("image_path", out var photoEl) ? photoEl.GetString() : null;
                var position = playerEl.TryGetProperty("position", out var posEl) ? posEl.GetString() : null;

                results.Add(new PlayerDto(
                    externalId,
                    teamExternalId,
                    playerEl.TryGetProperty("firstname", out var fnEl) ? fnEl.GetString() : null,
                    playerEl.TryGetProperty("lastname", out var lnEl) ? lnEl.GetString() : null,
                    displayName,
                    null,
                    null,
                    null,
                    position,
                    photo,
                    null,
                    teamName,
                    playerEl.GetRawText()));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sportmonks players sync failed.");
            return [];
        }
    }

    public Task<IReadOnlyList<PlayerStatsDto>> SyncPlayerStatsAsync(
        SyncStatsParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        // Sportmonks stats require per-player calls; defer to leaderboard sync for now.
        return Task.FromResult<IReadOnlyList<PlayerStatsDto>>([]);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopScorersAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return SyncLeaderboardAsync("goals", cancellationToken);
    }

    public Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopAssistsAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return SyncLeaderboardAsync("assists", cancellationToken);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> SyncLeaderboardAsync(
        string statType,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured || _options.SeasonId <= 0)
        {
            return [];
        }

        try
        {
            var url =
                $"{_options.BaseUrl.TrimEnd('/')}/topscorers/seasons/{_options.SeasonId}" +
                $"?api_token={_options.Token}&include=player;participant";
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sportmonks leaderboard request failed: {Status}", (int)response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<LeaderboardEntryDto>();
            var rank = 0;
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("player", out var playerEl))
                {
                    continue;
                }

                var externalId = playerEl.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
                if (string.IsNullOrWhiteSpace(externalId))
                {
                    continue;
                }

                var value = statType == "assists"
                    ? item.TryGetProperty("assists", out var assistEl) && assistEl.TryGetInt32(out var a) ? a : 0
                    : item.TryGetProperty("goals", out var goalsEl) && goalsEl.TryGetInt32(out var g) ? g : 0;

                if (value <= 0)
                {
                    continue;
                }

                rank++;
                var countryExternalId = item.TryGetProperty("participant", out var partEl) &&
                                        partEl.TryGetProperty("id", out var partIdEl)
                    ? partIdEl.GetRawText()
                    : null;

                results.Add(new LeaderboardEntryDto(
                    externalId,
                    countryExternalId,
                    rank,
                    value,
                    item.GetRawText()));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sportmonks leaderboard sync failed.");
            return [];
        }
    }
}
