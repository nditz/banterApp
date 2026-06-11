namespace BanterApp.Api.Integrations.News;

public interface INewsProvider
{
    Task<IReadOnlyList<NewsArticleDto>> GetLatestArticlesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
