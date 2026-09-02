namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Resolves a stable, renderable reaction GIF URL for a search query (e.g. "messi celebration").
/// Implementations return a persistable CDN URL, or null when unavailable/disabled.
/// </summary>
public interface IReactionGifProvider
{
    bool IsEnabled { get; }

    /// <summary>
    /// Finds a GIF for <paramref name="query"/>. <paramref name="seed"/> keeps a given feed
    /// card on the same GIF after the first pick; new cards receive a unique GIF for the
    /// current Friday–Monday window.
    /// </summary>
    Task<string?> FindGifUrlAsync(string query, int seed, CancellationToken cancellationToken = default);
}
