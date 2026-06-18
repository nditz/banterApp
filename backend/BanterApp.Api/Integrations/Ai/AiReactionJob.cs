using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// Medium-interval job: reads freshly ingested news &amp; match items and posts
/// AI pundit-style reactions with ChatGPT-picked GIFs or DALL-E images into the feed.
/// </summary>
public sealed class AiReactionJob
{
    public const string JobId = "ai-reactions";

    private readonly AppDbContext _db;
    private readonly IContentGenerator _ai;
    private readonly AiOptions _aiOptions;
    private readonly BackgroundJobsOptions _jobOptions;
    private readonly IApplicationErrorLogger _errorLogger;
    private readonly ILogger<AiReactionJob> _logger;

    public AiReactionJob(
        AppDbContext db,
        IContentGenerator ai,
        IOptions<AiOptions> aiOptions,
        IOptions<BackgroundJobsOptions> jobOptions,
        IApplicationErrorLogger errorLogger,
        ILogger<AiReactionJob> logger)
    {
        _db = db;
        _ai = ai;
        _aiOptions = aiOptions.Value;
        _jobOptions = jobOptions.Value;
        _errorLogger = errorLogger;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ReactAsync(CancellationToken cancellationToken)
    {
        if (!_aiOptions.Enabled)
        {
            return;
        }

        try
        {
            await ReactCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI reactions job failed.");
            await _errorLogger.LogExceptionAsync("background", ex, category: JobId, ct: cancellationToken);
        }
    }

    private async Task ReactCoreAsync(CancellationToken cancellationToken)
    {
        var reactedParentIds = await _db.NewsFeedItems
            .Where(n => n.ParentItemId != null)
            .Select(n => n.ParentItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var candidates = await _db.NewsFeedItems
            .Where(n => n.Category != "ai_reaction" && !reactedParentIds.Contains(n.Id))
            .OrderByDescending(n => n.PublishedAt)
            .Take(_jobOptions.AiReactionsBatchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            _logger.LogDebug("AI reactions: no new items to process.");
            return;
        }

        var created = 0;
        foreach (var item in candidates)
        {
            var reaction = await _ai.GenerateNewsReactionAsync(
                item.Title,
                item.Summary ?? item.Title,
                item.Category,
                cancellationToken);

            string? imageUrl = null;
            string? mediaType = null;

            try
            {
                var visual = await _ai.SuggestFeedVisualAsync(
                    item.Title,
                    reaction,
                    item.Category,
                    cancellationToken);

                if (visual.IsGif)
                {
                    imageUrl = FeedGifCatalog.ResolveGifUrl(visual.Mood, "news");
                    mediaType = "gif";
                }
                else if (visual.IsImage)
                {
                    var prompt = string.IsNullOrWhiteSpace(visual.ImagePrompt)
                        ? $"{item.Title}. {reaction}"
                        : visual.ImagePrompt;

                    imageUrl = await _ai.GenerateReactionImageUrlAsync(
                        item.Title,
                        prompt,
                        item.Category,
                        cancellationToken);

                    if (string.IsNullOrWhiteSpace(imageUrl) && _aiOptions.EnableImageGeneration)
                    {
                        imageUrl = await _ai.GenerateReactionImageUrlAsync(
                            item.Title,
                            reaction,
                            item.Category,
                            cancellationToken);
                    }

                    mediaType = string.IsNullOrWhiteSpace(imageUrl) ? null : "image";
                }

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    imageUrl = FeedGifCatalog.ResolveGifUrl(visual.Mood ?? "news");
                    mediaType = "gif";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI visual generation failed for feed item {ItemId}.", item.Id);
                imageUrl = FeedGifCatalog.ResolveGifUrl("news");
                mediaType = "gif";
            }

            _db.NewsFeedItems.Add(new NewsFeedItem
            {
                Id = $"ai-{Guid.NewGuid():N}",
                Source = "BanterApp AI",
                Title = $"Pundit take: {item.Title}",
                Summary = reaction,
                Url = item.Url,
                Author = "BanterBot",
                Category = "ai_reaction",
                ParentItemId = item.Id,
                ImageUrl = imageUrl,
                MediaType = mediaType,
                PublishedAt = DateTimeOffset.UtcNow,
                ViewCount = 0
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AI reactions: generated {Count} pundit posts with visuals.", created);
    }
}
