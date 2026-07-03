namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Configuration for the live reaction-GIF provider (Giphy by default). When no API key is present
/// the provider is disabled and the feed falls back to bundled local reaction stickers.
/// </summary>
public sealed class ReactionGifOptions
{
    public const string SectionName = "ReactionGif";

    /// <summary>giphy | tenor | none</summary>
    public string Provider { get; set; } = "giphy";

    public string? ApiKey { get; set; }

    /// <summary>Giphy API root (v1).</summary>
    public string GiphyBaseUrl { get; set; } = "https://api.giphy.com/v1";

    /// <summary>Giphy content rating: g | pg | pg-13 | r.</summary>
    public string ContentRating { get; set; } = "pg";

    /// <summary>Candidates fetched per query; one is chosen deterministically per feed card.</summary>
    public int SearchLimit { get; set; } = 12;

    // Legacy Tenor settings (optional fallback provider).
    public string ClientKey { get; set; } = "banterapp";

    public string TenorBaseUrl { get; set; } = "https://tenor.googleapis.com/v2";

    /// <summary>Tenor content safety: high | medium | low | off.</summary>
    public string ContentFilter { get; set; } = "high";

    public bool IsGiphyEnabled =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        string.Equals(Provider, "giphy", StringComparison.OrdinalIgnoreCase);

    public bool IsTenorEnabled =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        string.Equals(Provider, "tenor", StringComparison.OrdinalIgnoreCase);

    public bool Enabled => IsGiphyEnabled || IsTenorEnabled;
}
