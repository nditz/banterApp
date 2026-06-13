using System.Collections.Concurrent;
using BanterApp.Api.Data;
using BanterApp.Api.Integrations.News;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

public static class FeedEndpoints
{
    // In-memory reaction store keyed by feedItemId → (agree, stale, disagree)
    private static readonly ConcurrentDictionary<string, (int Agree, int Stale, int Disagree)> _reactions = new();

    private static readonly HashSet<string> ValidReactions = ["agree", "stale", "disagree"];

    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feed").WithTags("Feed");

        group.MapGet("/", GetFeed);
        group.MapGet("/trending", GetTrendingFeed);
        group.MapPost("/{id}/react", ReactToFeedItem).RequireRateLimiting("write");

        return app;
    }

    private static IResult ReactToFeedItem(string id, FeedReactRequest request)
    {
        var reaction = request.Reaction?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(reaction) || !ValidReactions.Contains(reaction))
        {
            return Results.BadRequest(new { error = "Reaction must be 'agree', 'stale', or 'disagree'." });
        }

        var counts = _reactions.AddOrUpdate(
            id,
            _ => reaction switch
            {
                "agree" => (1, 0, 0),
                "stale" => (0, 1, 0),
                _ => (0, 0, 1),
            },
            (_, existing) => reaction switch
            {
                "agree" => existing with { Agree = existing.Agree + 1 },
                "stale" => existing with { Stale = existing.Stale + 1 },
                _ => existing with { Disagree = existing.Disagree + 1 },
            });

        return Results.Ok(new FeedReactions(counts.Agree, counts.Stale, counts.Disagree));
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

    private static FeedReactions? GetReactions(string id) =>
        _reactions.TryGetValue(id, out var r) ? new FeedReactions(r.Agree, r.Stale, r.Disagree) : null;

    private static FeedItemResponse MapFromEntity(Data.Entities.NewsFeedItem n)
    {
        var sid = n.Id.ToString();
        return new(sid, "news", n.Title, n.Summary ?? n.Title, null, n.Source, n.Url, n.PublishedAt, n.ViewCount, GetReactions(sid));
    }

    private static FeedItemResponse MapFromDto(NewsArticleDto a, int? likes = null) =>
        new(a.Id, "news", a.Title, a.Summary, a.ImageUrl, a.SourceName, a.SourceUrl, a.PublishedAt,
            likes ?? Random.Shared.Next(100, 5000), GetReactions(a.Id));
}
