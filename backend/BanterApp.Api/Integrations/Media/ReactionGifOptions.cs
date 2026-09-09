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

    /// <summary>
    /// Candidates fetched per Giphy search page when random GIFs collide with ones already
    /// shown this Friday–Monday window.
    /// </summary>
    public int SearchLimit { get; set; } = 25;

    /// <summary>
    /// When true, timeline write-path prefers our first-party library before a live Giphy/Tenor call.
    /// </summary>
    public bool PreferLocalLibrary { get; set; } = true;

    /// <summary>
    /// Chance (0–1) to try a live Giphy lookup even when the local library has a match,
    /// so the feed still mixes in fresh licensed CDN URLs without hitting the API every card.
    /// </summary>
    public double LiveMixChance { get; set; } = 0.25;

    /// <summary>Max live provider lookups per card resolve (each lookup can be several HTTP calls).</summary>
    public int MaxLiveLookupsPerResolve { get; set; } = 1;

    /// <summary>Giphy trending-search + tag requests allowed per refresh job run.</summary>
    public int QueryRefreshMaxCalls { get; set; } = 2;

    public string UsageProviderName =>
        IsTenorEnabled ? "tenor" : "giphy";

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
