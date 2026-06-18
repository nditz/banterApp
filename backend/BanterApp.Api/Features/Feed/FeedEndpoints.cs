using System.Collections.Concurrent;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Integrations.News;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Feed;

public static class FeedEndpoints
{
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
        IUserContext user,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 20, 1, 50);
        var skip = (currentPage - 1) * size;

        var (feedMode, personal) = await PersonalizedFeedService.BuildAsync(db, user, 20, ct);
        var newsItems = await LoadNewsItemsAsync(db, news, 100, ct);
        var merged = personal
            .Concat(newsItems)
            .OrderByDescending(i => i.PublishedAt)
            .ToList();

        var pageItems = merged.Skip(skip).Take(size).ToList();
        return Results.Ok(BuildPage(pageItems, currentPage, size, merged.Count, feedMode));
    }

    private static async Task<IResult> GetTrendingFeed(
        int? page,
        int? pageSize,
        int? count,
        AppDbContext db,
        INewsProvider news,
        IUserContext user,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 10, 1, 30);
        var skip = (currentPage - 1) * size;

        var (feedMode, personal) = await PersonalizedFeedService.BuildAsync(db, user, 10, ct);
        var newsItems = await LoadNewsItemsAsync(db, news, 100, ct);
        var merged = personal
            .Concat(newsItems)
            .OrderByDescending(i => i.Likes ?? 0)
            .ThenByDescending(i => i.PublishedAt)
            .ToList();

        var pageItems = merged.Skip(skip).Take(size).ToList();
        return Results.Ok(BuildPage(pageItems, currentPage, size, merged.Count, feedMode));
    }

    private static async Task<List<FeedItemResponse>> LoadNewsItemsAsync(
        AppDbContext db,
        INewsProvider news,
        int maxItems,
        CancellationToken ct)
    {
        var items = await db.NewsFeedItems
            .OrderByDescending(n => n.PublishedAt)
            .Take(maxItems)
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            return items.Select(MapFromEntity).ToList();
        }

        var articles = await news.GetLatestArticlesAsync(maxItems, ct);
        return articles
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => MapFromDto(a))
            .ToList();
    }

    private static PaginatedFeedResponse BuildPage(
        IReadOnlyList<FeedItemResponse> items,
        int page,
        int pageSize,
        int totalCount,
        string? feedMode = null) =>
        new(
            items,
            page,
            pageSize,
            totalCount,
            page * pageSize < totalCount,
            feedMode);

    private static FeedReactions? GetReactions(string id) =>
        _reactions.TryGetValue(id, out var r) ? new FeedReactions(r.Agree, r.Stale, r.Disagree) : null;

    private static string MapCategoryToType(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "banter" or "meme" or "prediction_highlight" or "leaderboard" or "ai_reaction" =>
                category.Trim().ToLowerInvariant() switch
                {
                    "ai_reaction" => "banter",
                    _ => category.Trim().ToLowerInvariant(),
                },
            _ => "news",
        };

    private static FeedItemResponse MapFromEntity(Data.Entities.NewsFeedItem n)
    {
        var sid = n.Id.ToString();
        var media = FeedMediaMapper.FromNewsItem(n);
        return new(
            sid,
            MapCategoryToType(n.Category),
            n.Title,
            n.Summary ?? n.Title,
            n.ImageUrl,
            n.Source,
            n.Url,
            n.PublishedAt,
            n.ViewCount,
            GetReactions(sid),
            media);
    }

    private static FeedItemResponse MapFromDto(NewsArticleDto a, int? likes = null)
    {
        var media = string.IsNullOrWhiteSpace(a.ImageUrl)
            ? null
            : FeedMediaMapper.FromImageUrl(a.ImageUrl, a.Title);
        return new(
            a.Id,
            "news",
            a.Title,
            a.Summary,
            a.ImageUrl,
            a.SourceName,
            a.SourceUrl,
            a.PublishedAt,
            likes ?? Random.Shared.Next(100, 5000),
            GetReactions(a.Id),
            media);
    }
}
