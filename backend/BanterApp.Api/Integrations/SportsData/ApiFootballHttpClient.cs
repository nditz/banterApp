using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.SportsData;

/// <summary>
/// HTTP client for API-Football with retry and rate-limit handling.
/// </summary>
public sealed class ApiFootballHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly SportsDataOptions _options;
    private readonly ILogger<ApiFootballHttpClient> _logger;

    public ApiFootballHttpClient(
        HttpClient httpClient,
        IOptions<SportsDataOptions> options,
        ILogger<ApiFootballHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<JsonDocument?> GetJsonAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!HasApiKey)
        {
            return null;
        }

        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"{_options.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var request = CreateRequest(url);
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    _logger.LogWarning("API-Football rate limited; retrying in {Delay}s.", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "API-Football request failed: {Status} {Url}",
                        (int)response.StatusCode,
                        url);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (attempt < 2)
            {
                _logger.LogWarning(ex, "API-Football request error (attempt {Attempt}); retrying.", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
        }

        return null;
    }

    private HttpRequestMessage CreateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-apisports-key", _options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
