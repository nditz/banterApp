namespace BanterApp.Api.Integrations.Media;

/// <summary>No-op provider used when no reaction-GIF API key is configured.</summary>
public sealed class NullReactionGifProvider : IReactionGifProvider
{
    public bool IsEnabled => false;

    public Task<string?> FindGifUrlAsync(
        string query,
        int seed,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
