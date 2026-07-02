using System.Collections.Concurrent;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
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

        group.MapGet("/", GetFeed).RequireRateLimiting(RateLimitPolicies.PublicFeed);
        group.MapGet("/trending", GetTrendingFeed).RequireRateLimiting(RateLimitPolicies.PublicFeed);
        group.MapPost("/{id}/react", ReactToFeedItem).RequireRateLimiting(RateLimitPolicies.PublicReactions);

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
        HttpContext http,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 20, 1, 50);
        var skip = (currentPage - 1) * size;

        var (feedMode, personal) = await PersonalizedFeedService.BuildAsync(db, user, 20, ct);
        var newsItems = await LoadNewsItemsAsync(db, news, 100, ct);
        var merged = DedupeAndVaryMedia(personal
            .Concat(newsItems)
            .OrderByDescending(i => i.PublishedAt));

        var pageItems = merged.Skip(skip).Take(size).ToList();
        http.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(BuildPage(pageItems, currentPage, size, merged.Count, feedMode));
    }

    private static async Task<IResult> GetTrendingFeed(
        int? page,
        int? pageSize,
        int? count,
        AppDbContext db,
        INewsProvider news,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? count ?? 10, 1, 30);
        var skip = (currentPage - 1) * size;

        var (feedMode, personal) = await PersonalizedFeedService.BuildAsync(db, user, 10, ct);
        var newsItems = await LoadNewsItemsAsync(db, news, 100, ct);
        var merged = DedupeAndVaryMedia(personal
            .Concat(newsItems)
            .OrderByDescending(i => i.Likes ?? 0)
            .ThenByDescending(i => i.PublishedAt));

        var pageItems = merged.Skip(skip).Take(size).ToList();
        http.Response.Headers.CacheControl = "public, max-age=60";
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
            var mapped = items.Select(MapFromEntity).ToList();
            await AppendMissingPunditOpinionItemsAsync(db, mapped, maxItems, ct);
            return mapped;
        }

        var articles = await news.GetLatestArticlesAsync(maxItems, ct);
        return articles
            .OrderByDescending(a => a.PublishedAt)
            .Select(a => MapFromDto(a))
            .ToList();
    }

    /// <summary>
    /// Removes duplicate feed items (same Id can be added by both the personalized
    /// builder and the persisted news list) and, for GIFs, swaps a repeated media URL
    /// to an alternate from the same mood pool so the feed does not show the same GIF twice.
    /// </summary>
    private static List<FeedItemResponse> DedupeAndVaryMedia(IEnumerable<FeedItemResponse> items)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedMedia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<FeedItemResponse>();

        foreach (var item in items)
        {
            if (!seenIds.Add(item.Id))
            {
                continue;
            }

            var current = item;
            var url = current.Media?.Url;

            if (!string.IsNullOrWhiteSpace(url) &&
                string.Equals(current.Media!.Type, "gif", StringComparison.OrdinalIgnoreCase) &&
                usedMedia.Contains(url))
            {
                var alternate = FeedGifCatalog.ResolveAlternate(url, usedMedia);
                if (!string.Equals(alternate, url, StringComparison.Ordinal))
                {
                    current = current with
                    {
                        Media = current.Media with { Url = alternate },
                        ImageUrl = string.Equals(current.ImageUrl, url, StringComparison.Ordinal)
                            ? alternate
                            : current.ImageUrl,
                    };
                    url = alternate;
                }
            }

            if (!string.IsNullOrWhiteSpace(url))
            {
                usedMedia.Add(url);
            }

            result.Add(current);
        }

        return result;
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

    private static string MapCategoryToType(string? category, bool isBanterized) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "pundit_quote" when isBanterized => "banter",
            "pundit_quote" => "pundit_quote",
            "banter" or "meme" or "prediction_highlight" or "leaderboard" or "ai_reaction" =>
                category.Trim().ToLowerInvariant() switch
                {
                    "ai_reaction" => "banter",
                    _ => category.Trim().ToLowerInvariant(),
                },
            _ when isBanterized => "banter",
            _ => "news",
        };

    private static FeedItemResponse MapFromEntity(Data.Entities.NewsFeedItem n)
    {
        var sid = n.Id.ToString();
        var isBanterized = FeedBanterFormat.IsBanterized(n.Summary) || FeedBanterFormat.IsBanterized(n.Title);
        var title = FeedBanterFormat.Strip(n.Title);
        var summary = FeedBanterFormat.Strip(n.Summary ?? n.Title);
        var media = FeedMediaMapper.FromNewsItem(n);
        var type = MapCategoryToType(n.Category, isBanterized);

        return new(
            sid,
            type,
            title,
            summary,
            n.ImageUrl,
            n.Source,
            n.Url,
            n.PublishedAt,
            n.ViewCount,
            GetReactions(sid),
            media,
            n.Author,
            isBanterized ? "ai_summary" : "news");
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
            media,
            ContentLabel: "news");
    }

    private static async Task AppendMissingPunditOpinionItemsAsync(
        AppDbContext db,
        List<FeedItemResponse> target,
        int maxItems,
        CancellationToken ct)
    {
        var existingIds = target.Select(i => i.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var opinions = await db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.Pundit.Kind == PunditKind.Source && !o.NeedsHumanReview && o.ReviewStatus != "rejected")
            .OrderByDescending(o => o.SourceItem.PublishedAt ?? o.CreatedAt)
            .Take(maxItems)
            .ToListAsync(ct);

        foreach (var opinion in opinions)
        {
            var feedItem = PunditOpinionFeedMapper.ToFeedItem(opinion, opinion.Pundit, opinion.SourceItem);
            if (existingIds.Add(feedItem.Id))
            {
                target.Add(feedItem);
            }
        }
    }
}
