using BanterApp.Api.Data;
using BanterApp.Api.Integrations.News;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feed").WithTags("Feed");

        group.MapGet("/", GetFeed);
        group.MapGet("/trending", GetTrendingFeed);

        return app;
    }

    private static async Task<IResult> GetFeed(
        int? page,
        int? pageSize,
        int? count,
        AppDbContext db,
        INewsProvider news,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 20, 1, 50);
        var skip = (currentPage - 1) * size;

        var query = db.NewsFeedItems
            .OrderByDescending(n => n.PublishedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(size).ToListAsync(ct);

        if (totalCount == 0)
        {
            var articles = await news.GetLatestArticlesAsync(100, ct);
            totalCount = articles.Count;
            var pageItems = articles
                .OrderByDescending(a => a.PublishedAt)
                .Skip(skip)
                .Take(size)
                .Select(a => MapFromDto(a))
                .ToList();

            return Results.Ok(BuildPage(pageItems, currentPage, size, totalCount));
        }

        var mapped = items.Select(MapFromEntity).ToList();
        return Results.Ok(BuildPage(mapped, currentPage, size, totalCount));
    }

    private static async Task<IResult> GetTrendingFeed(
        int? page,
        int? pageSize,
        int? count,
        AppDbContext db,
        INewsProvider news,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 10, 1, 30);
        var skip = (currentPage - 1) * size;

        var query = db.NewsFeedItems
            .OrderByDescending(n => n.ViewCount)
            .ThenByDescending(n => n.PublishedAt)
            .AsQueryable();

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(size).ToListAsync(ct);

        if (totalCount == 0)
        {
            var articles = await news.GetLatestArticlesAsync(100, ct);
            totalCount = articles.Count;
            var pageItems = articles
                .OrderByDescending(a => a.PublishedAt)
                .Skip(skip)
                .Take(size)
                .Select(a => MapFromDto(a, Random.Shared.Next(500, 5000)))
                .ToList();

            return Results.Ok(BuildPage(pageItems, currentPage, size, totalCount));
        }

        var mapped = items.Select(MapFromEntity).ToList();
        return Results.Ok(BuildPage(mapped, currentPage, size, totalCount));
    }

    private static PaginatedFeedResponse BuildPage(
        IReadOnlyList<FeedItemResponse> items,
        int page,
        int pageSize,
        int totalCount) =>
        new(
            items,
            page,
            pageSize,
            totalCount,
            page * pageSize < totalCount);

    private static FeedItemResponse MapFromEntity(Data.Entities.NewsFeedItem n) =>
        new(
            n.Id.ToString(),
            "news",
            n.Title,
            n.Summary ?? n.Title,
            null,
            n.Source,
            n.Url,
            n.PublishedAt,
            n.ViewCount);

    private static FeedItemResponse MapFromDto(NewsArticleDto a, int? likes = null) =>
        new(
            a.Id,
            "news",
            a.Title,
            a.Summary,
            a.ImageUrl,
            a.SourceName,
            a.SourceUrl,
            a.PublishedAt,
            likes ?? Random.Shared.Next(100, 5000));
}
