using System.Text.Json;
using System.Text.Json.Serialization;

namespace BanterApp.Api.Services;

public sealed class TurnstileService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        var secret = configuration["Security:TurnstileSecretKey"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        using var client = httpClientFactory.CreateClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token,
            ["remoteip"] = remoteIp ?? string.Empty
        });

        using var response = await client.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify",
            content,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<TurnstileResponse>(stream, cancellationToken: ct);
        return result?.Success == true;
    }

    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
