namespace BanterApp.Api.Features.Feed;

public sealed record FeedReactions(int Agree, int Stale, int Disagree);

public sealed record FeedMediaResponse(
    string Type,
    string Url,
    string? Alt = null);

public sealed record FeedItemResponse(
    string Id,
    string Type,
    string Title,
    string Body,
    string? ImageUrl,
    string? Source,
    string? SourceUrl,
    DateTimeOffset PublishedAt,
    int? Likes,
    FeedReactions? Reactions = null,
    FeedMediaResponse? Media = null,
    string? Author = null,
    string? ContentLabel = null,
    string? MatchId = null,
    string? Prediction = null,
    double? Confidence = null,
    int? QualityScore = null);

public sealed record PaginatedFeedResponse(
    IReadOnlyList<FeedItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore,
    string? FeedMode = null);

public record FeedReactRequest(string Reaction);
