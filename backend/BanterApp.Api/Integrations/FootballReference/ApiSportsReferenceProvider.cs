using System.Text.Json;
using BanterApp.Api.Integrations.FootballReference.Dtos;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Integrations.SportsData.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.FootballReference;

public sealed class ApiSportsReferenceProvider : IFootballReferenceDataProvider
{
    private readonly ApiFootballHttpClient _client;
    private readonly FootballReferenceDataOptions _options;
    private readonly ILogger<ApiSportsReferenceProvider> _logger;

    public ApiSportsReferenceProvider(
        ApiFootballHttpClient client,
        IOptions<FootballReferenceDataOptions> options,
        ILogger<ApiSportsReferenceProvider> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "api_sports";

    public bool IsConfigured => _client.HasApiKey;

    public async Task<IReadOnlyList<CountryDto>> SyncCountriesAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        using var doc = await _client.GetJsonAsync("countries", cancellationToken);
        if (doc is null)
        {
            return [];
        }

        var results = new List<CountryDto>();
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            response.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in response.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameEl))
            {
                continue;
            }

            var name = nameEl.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var externalId = item.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : name;
            var code = item.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
            var flag = item.TryGetProperty("flag", out var flagEl) ? flagEl.GetString() : null;
            var continent = item.TryGetProperty("continent", out var contEl) ? contEl.GetString() : null;

            results.Add(new CountryDto(
                externalId,
                name,
                code,
                flag,
                continent,
                null,
                item.GetRawText()));
        }

        return results;
    }

    public async Task<IReadOnlyList<PlayerDto>> SyncPlayersAsync(
        SyncPlayersParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var leagueId = parameters?.LeagueId ?? _options.LeagueId;
        var season = parameters?.Season ?? _options.Season;
        using var teamsDoc = await _client.GetJsonAsync(
            $"teams?league={leagueId}&season={season}",
            cancellationToken);
        if (teamsDoc is null)
        {
            return [];
        }

        var teams = ApiFootballFixtureMapper.MapTeams(teamsDoc.RootElement);
        var results = new List<PlayerDto>();

        foreach (var team in teams)
        {
            using var squadDoc = await _client.GetJsonAsync(
                $"players/squads?team={team.Id}",
                cancellationToken);
            if (squadDoc is null)
            {
                continue;
            }

            var squad = ApiFootballFixtureMapper.MapSquad(squadDoc.RootElement, team.Id);
            if (squad is null)
            {
                continue;
            }

            foreach (var player in squad.Players)
            {
                results.Add(new PlayerDto(
                    player.ProviderPlayerId,
                    null,
                    null,
                    null,
                    player.Name,
                    null,
                    null,
                    null,
                    player.Position,
                    null,
                    team.Name,
                    null,
                    null));
            }
        }

        _logger.LogInformation(
            "API-Sports player sync loaded {Players} players across {Teams} clubs.",
            results.Count,
            teams.Count);

        return results;
    }

    public async Task<IReadOnlyList<PlayerStatsDto>> SyncPlayerStatsAsync(
        SyncStatsParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var leagueId = parameters?.LeagueId ?? _options.LeagueId;
        var season = parameters?.Season ?? _options.Season;
        var competition = parameters?.Competition ?? _options.CompetitionCode;
        var path = $"players/topscorers?league={leagueId}&season={season}";

        using var doc = await _client.GetJsonAsync(path, cancellationToken);
        if (doc is null)
        {
            return [];
        }

        var results = new List<PlayerStatsDto>();
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            response.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var block in response.EnumerateArray())
        {
            if (!block.TryGetProperty("players", out var players) ||
                players.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in players.EnumerateArray())
            {
                if (!entry.TryGetProperty("player", out var playerEl))
                {
                    continue;
                }

                var externalId = playerEl.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
                if (string.IsNullOrWhiteSpace(externalId))
                {
                    continue;
                }

                var stats = entry.TryGetProperty("statistics", out var statsEl) &&
                            statsEl.ValueKind == JsonValueKind.Array &&
                            statsEl.GetArrayLength() > 0
                    ? statsEl[0]
                    : default;

                var goals = 0;
                var assists = 0;
                var matches = 0;
                var minutes = 0;
                var yellow = 0;
                var red = 0;
                decimal? rating = null;

                if (stats.ValueKind == JsonValueKind.Object)
                {
                    if (stats.TryGetProperty("goals", out var goalsEl))
                    {
                        goals = goalsEl.TryGetProperty("total", out var totalEl) &&
                                totalEl.TryGetInt32(out var g)
                            ? g
                            : 0;
                        assists = goalsEl.TryGetProperty("assists", out var assistEl) &&
                                  assistEl.TryGetInt32(out var a)
                            ? a
                            : 0;
                    }

                    if (stats.TryGetProperty("games", out var gamesEl))
                    {
                        matches = gamesEl.TryGetProperty("appearences", out var appEl) &&
                                  appEl.TryGetInt32(out var m)
                            ? m
                            : gamesEl.TryGetProperty("appearences", out var altEl) &&
                              altEl.TryGetInt32(out var m2)
                                ? m2
                                : 0;
                        minutes = gamesEl.TryGetProperty("minutes", out var minEl) &&
                                  minEl.TryGetInt32(out var mins)
                            ? mins
                            : 0;
                        if (gamesEl.TryGetProperty("rating", out var ratingEl) &&
                            decimal.TryParse(ratingEl.GetString(), out var r))
                        {
                            rating = r;
                        }
                    }

                    if (stats.TryGetProperty("cards", out var cardsEl))
                    {
                        yellow = cardsEl.TryGetProperty("yellow", out var yEl) &&
                                 yEl.TryGetInt32(out var y)
                            ? y
                            : 0;
                        red = cardsEl.TryGetProperty("red", out var rEl) &&
                              rEl.TryGetInt32(out var rv)
                            ? rv
                            : 0;
                    }
                }

                var countryExternalId = stats.ValueKind == JsonValueKind.Object &&
                                        stats.TryGetProperty("team", out var teamEl) &&
                                        teamEl.TryGetProperty("id", out var teamIdEl)
                    ? teamIdEl.GetRawText()
                    : null;

                results.Add(new PlayerStatsDto(
                    externalId,
                    countryExternalId,
                    competition,
                    season,
                    matches,
                    goals,
                    assists,
                    yellow,
                    red,
                    minutes,
                    rating,
                    entry.GetRawText()));
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopScorersAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return await SyncLeaderboardAsync("players/topscorers", parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> SyncTopAssistsAsync(
        LeaderboardParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return await SyncLeaderboardAsync("players/topassists", parameters, cancellationToken);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> SyncLeaderboardAsync(
        string endpoint,
        LeaderboardParams? parameters,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var leagueId = parameters?.LeagueId ?? _options.LeagueId;
        var season = parameters?.Season ?? _options.Season;
        var path = $"{endpoint}?league={leagueId}&season={season}";

        using var doc = await _client.GetJsonAsync(path, cancellationToken);
        if (doc is null)
        {
            return [];
        }

        var results = new List<LeaderboardEntryDto>();
        if (!doc.RootElement.TryGetProperty("response", out var response) ||
            response.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        var rank = 0;
        foreach (var block in response.EnumerateArray())
        {
            if (!block.TryGetProperty("players", out var players) ||
                players.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in players.EnumerateArray())
            {
                rank++;
                if (!entry.TryGetProperty("player", out var playerEl))
                {
                    continue;
                }

                var externalId = playerEl.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
                if (string.IsNullOrWhiteSpace(externalId))
                {
                    continue;
                }

                var value = 0m;
                if (entry.TryGetProperty("statistics", out var statsEl) &&
                    statsEl.ValueKind == JsonValueKind.Array &&
                    statsEl.GetArrayLength() > 0 &&
                    statsEl[0].TryGetProperty("goals", out var goalsEl))
                {
                    if (endpoint.Contains("topassists", StringComparison.OrdinalIgnoreCase))
                    {
                        value = goalsEl.TryGetProperty("assists", out var assistEl) &&
                                assistEl.TryGetInt32(out var a)
                            ? a
                            : 0;
                    }
                    else
                    {
                        value = goalsEl.TryGetProperty("total", out var totalEl) &&
                                totalEl.TryGetInt32(out var g)
                            ? g
                            : 0;
                    }
                }

                var countryExternalId = entry.TryGetProperty("statistics", out var stats2) &&
                                        stats2.ValueKind == JsonValueKind.Array &&
                                        stats2.GetArrayLength() > 0 &&
                                        stats2[0].TryGetProperty("team", out var teamEl) &&
                                        teamEl.TryGetProperty("id", out var teamIdEl)
                    ? teamIdEl.GetRawText()
                    : null;

                results.Add(new LeaderboardEntryDto(
                    externalId,
                    countryExternalId,
                    rank,
                    value,
                    entry.GetRawText()));
            }
        }

        return results;
    }
}
