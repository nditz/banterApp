namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Configuration for the live reaction-GIF provider (Tenor). When no API key is present the
/// provider is disabled and the feed falls back to the bundled local reaction stickers.
/// </summary>
public sealed class ReactionGifOptions
{
    public const string SectionName = "ReactionGif";

    /// <summary>tenor | none</summary>
    public string Provider { get; set; } = "tenor";

    public string? ApiKey { get; set; }

    /// <summary>Tenor requires a stable, app-specific client key for anonymous integrations.</summary>
    public string ClientKey { get; set; } = "banterapp";

    public string BaseUrl { get; set; } = "https://tenor.googleapis.com/v2";

    /// <summary>Tenor content safety: high | medium | low | off.</summary>
    public string ContentFilter { get; set; } = "high";

    /// <summary>Candidates fetched per query; one is chosen deterministically per feed card.</summary>
    public int SearchLimit { get; set; } = 12;

    public bool Enabled =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        string.Equals(Provider, "tenor", StringComparison.OrdinalIgnoreCase);
}
