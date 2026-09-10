namespace BanterApp.Api.Features.Studio;

public sealed record StudioStoryCard(
    string Id,
    string Kind,
    string Title,
    string Summary,
    IReadOnlyList<string> Tags,
    DateTimeOffset? OccurredAt,
    Guid? ReceiptId = null,
    string? MatchId = null,
    string? FeedItemId = null,
    Guid? ProjectId = null,
    string? Scoreline = null,
    string? StoryType = null);

public sealed record StudioStoriesResponse(
    IReadOnlyList<StudioStoryCard> LatestReceipts,
    IReadOnlyList<StudioStoryCard> YouVsPundits,
    IReadOnlyList<StudioStoryCard> Trending,
    IReadOnlyList<StudioStoryCard> PreviousProjects);

public sealed record StudioFact(
    string Label,
    string Value,
    string Provenance);

public sealed record StudioContentPack(
    Guid Id,
    string ContentType,
    string Tone,
    string Title,
    string Hook,
    string Script,
    IReadOnlyList<StudioFact> Facts,
    IReadOnlyList<string> VisualPlan,
    string? MemeDirection,
    string Caption,
    IReadOnlyList<string> Hashtags,
    string ImagePrompt,
    string VoiceoverPrompt,
    IReadOnlyList<string> SourceNotes,
    string StoryKind,
    Guid? ReceiptId,
    string? MatchId,
    string? FeedItemId,
    DateTimeOffset CreatedAt);

public sealed record CreateStudioPackRequest(
    string ContentType,
    string Tone,
    Guid? ReceiptId = null,
    string? FeedItemId = null,
    Guid? ProjectId = null);

public sealed record StudioPackGenerateResponse(
    StudioContentPack Pack,
    int? RemainingGenerations);
