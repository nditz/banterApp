using BanterApp.Api.Integrations.Pundits.Dtos;

namespace BanterApp.Api.Integrations.Pundits;

/// <summary>
/// No-op extractor. Fabricating pundit takes from missing transcripts is not allowed.
/// </summary>
public sealed class StubPunditOpinionExtractor : IPunditOpinionExtractor
{
    public Task<PunditExtractionResult?> ExtractAsync(
        string sourceType,
        string sourceName,
        string sourceUrl,
        string sourceTitle,
        DateTimeOffset? publishedAt,
        string? author,
        string sourceText,
        CancellationToken cancellationToken = default)
    {
        _ = sourceType;
        _ = sourceName;
        _ = sourceUrl;
        _ = sourceTitle;
        _ = publishedAt;
        _ = author;
        _ = sourceText;
        _ = cancellationToken;
        return Task.FromResult<PunditExtractionResult?>(null);
    }
}
