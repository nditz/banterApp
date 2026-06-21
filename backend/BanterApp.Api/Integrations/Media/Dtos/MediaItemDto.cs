namespace BanterApp.Api.Integrations.Media.Dtos;

public sealed record MediaItemDto(
    string ExternalId,
    string Title,
    string? Description,
    string SourceUrl,
    string? AudioUrl,
    DateTimeOffset? PublishedAt,
    string SourceExternalId,
    string? Author = null,
    string? Publication = null,
    string? FullText = null);
