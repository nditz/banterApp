using BanterApp.Api.Integrations.Media.Dtos;

namespace BanterApp.Api.Integrations.Media;

public sealed record RssFeedFetchFailure(
    string Reason,
    string SafeMessage,
    string? Detail,
    bool SsrfBlocked,
    int? StatusCode);

public sealed record RssFeedFetchResult(
    IReadOnlyList<MediaItemDto> Items,
    string FeedUrl,
    RssFeedFetchFailure? Failure = null)
{
    public bool IsSuccess => Failure is null;

    public static RssFeedFetchResult Empty(string feedUrl) => new([], feedUrl);

    public static RssFeedFetchResult Ok(IReadOnlyList<MediaItemDto> items, string feedUrl) =>
        new(items, feedUrl);

    public static RssFeedFetchResult Failed(string feedUrl, RssFeedFetchFailure failure) =>
        new([], feedUrl, failure);
}
