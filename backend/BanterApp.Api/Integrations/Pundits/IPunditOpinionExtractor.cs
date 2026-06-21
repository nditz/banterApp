using BanterApp.Api.Integrations.Pundits.Dtos;

namespace BanterApp.Api.Integrations.Pundits;

public interface IPunditOpinionExtractor
{
    Task<PunditExtractionResult?> ExtractAsync(
        string sourceType,
        string sourceName,
        string sourceUrl,
        string sourceTitle,
        DateTimeOffset? publishedAt,
        string? author,
        string sourceText,
        CancellationToken cancellationToken = default);
}
