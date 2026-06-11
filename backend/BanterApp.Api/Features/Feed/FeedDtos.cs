namespace BanterApp.Api.Features.Feed;

public sealed record FeedItemResponse(
    string Id,
    string Type,
    string Title,
    string Body,
    string? ImageUrl,
    string? Source,
    string? SourceUrl,
    DateTimeOffset PublishedAt,
    int? Likes);

public sealed record PaginatedFeedResponse(
    IReadOnlyList<FeedItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
