using BanterApp.Api.Data.Entities;

namespace BanterApp.Api.Integrations.Rss;

public interface IRssUrlDiscovery
{
    Task<string?> SuggestFeedUrlAsync(
        RssFeed feed,
        string failureReason,
        CancellationToken cancellationToken = default);
}

public sealed class NullRssUrlDiscovery : IRssUrlDiscovery
{
    public static readonly NullRssUrlDiscovery Instance = new();

    public Task<string?> SuggestFeedUrlAsync(
        RssFeed feed,
        string failureReason,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
