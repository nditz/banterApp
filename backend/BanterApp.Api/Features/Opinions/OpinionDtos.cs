namespace BanterApp.Api.Features.Opinions;

public sealed record OpinionResponseDto(
    Guid Id,
    string Pundit,
    string Opinion,
    string? Prediction,
    string? Team,
    string? Player,
    string SourceName,
    string SourceTitle,
    string SourceUrl,
    DateTimeOffset? PublishedAt,
    string? EvidenceQuote,
    bool IsDirectQuote,
    double? Confidence,
    bool NeedsHumanReview);

public sealed record PunditSummaryDto(
    Guid Id,
    string Name,
    string? Role,
    string? Organization,
    int OpinionCount);

public sealed record SourceSummaryDto(
    Guid Id,
    string Name,
    string SourceType,
    string? Url,
    bool Enabled,
    int ItemCount);

public sealed record SourceItemDetailDto(
    Guid Id,
    string Title,
    string SourceUrl,
    string? Author,
    string? Publication,
    DateTimeOffset? PublishedAt,
    string ProcessingStatus,
    IReadOnlyList<OpinionResponseDto> Opinions);

public sealed record PunditPredictionAggregateDto(
    string EntityType,
    string EntityName,
    string PredictionType,
    string? ConsensusSummary,
    int PositiveCount,
    int NegativeCount,
    int NeutralCount,
    int SourceCount,
    double ConfidenceScore,
    DateTimeOffset UpdatedAt);
