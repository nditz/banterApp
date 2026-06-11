namespace BanterApp.Api.Integrations.News;

public sealed record NewsArticleDto(
    string Id,
    string Title,
    string Summary,
    string SourceName,
    string SourceUrl,
    string? Author,
    DateTimeOffset PublishedAt,
    string? ImageUrl = null,
    string? Category = null)
{
    public string Source => SourceName;

    public string Url => SourceUrl;
}
