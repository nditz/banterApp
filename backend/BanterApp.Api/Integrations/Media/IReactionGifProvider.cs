namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Resolves a stable, renderable reaction GIF URL for a search query (e.g. "messi celebration").
/// Implementations return a persistable CDN URL, or null when unavailable/disabled.
/// </summary>
public interface IReactionGifProvider
{
    bool IsEnabled { get; }

    /// <summary>
    /// Finds a GIF for <paramref name="query"/>. <paramref name="seed"/> deterministically selects
    /// among candidates so a given feed card stays stable while different cards vary.
    /// </summary>
    Task<string?> FindGifUrlAsync(string query, int seed, CancellationToken cancellationToken = default);
}
