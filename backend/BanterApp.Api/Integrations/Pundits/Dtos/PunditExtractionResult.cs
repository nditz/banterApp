using BanterApp.Api.Integrations.Media.Dtos;

namespace BanterApp.Api.Integrations.Pundits.Dtos;

public sealed record PunditExtractionOpinionDto(
    string Topic,
    string? Team,
    string? Player,
    string? Match,
    string? MatchId,
    string Opinion,
    string? Prediction,
    string PredictionType,
    double Confidence,
    string? EvidenceQuote,
    string? QuoteContext,
    bool IsDirectQuote,
    bool NeedsHumanReview);

public sealed record PunditExtractionPunditDto(
    string Name,
    string Role,
    IReadOnlyList<PunditExtractionOpinionDto> Opinions);

public sealed record PunditExtractionResult(
    string SourceType,
    string SourceName,
    string SourceUrl,
    string SourceTitle,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<PunditExtractionPunditDto> Pundits,
    IReadOnlyList<string> MissingInformation,
    string Summary,
    string RawJson);
