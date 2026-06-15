using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// Medium-interval job: reads freshly ingested news &amp; match items and posts
/// AI pundit-style reactions into the rolling feed.
/// </summary>
public sealed class AiReactionJob
{
    public const string JobId = "ai-reactions";

    private readonly AppDbContext _db;
    private readonly IContentGenerator _ai;
    private readonly AiOptions _aiOptions;
    private readonly BackgroundJobsOptions _jobOptions;
    private readonly ILogger<AiReactionJob> _logger;

    public AiReactionJob(
        AppDbContext db,
        IContentGenerator ai,
        IOptions<AiOptions> aiOptions,
        IOptions<BackgroundJobsOptions> jobOptions,
        ILogger<AiReactionJob> logger)
    {
        _db = db;
        _ai = ai;
        _aiOptions = aiOptions.Value;
        _jobOptions = jobOptions.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task ReactAsync(CancellationToken cancellationToken)
    {
        if (!_aiOptions.Enabled)
        {
            return;
        }

        // Items that already have an AI reaction child
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
            try
            {
                imageUrl = await _ai.GenerateReactionImageUrlAsync(
                    item.Title,
                    reaction,
                    item.Category,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI image generation failed for feed item {ItemId}.", item.Id);
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
                PublishedAt = DateTimeOffset.UtcNow,
                ViewCount = 0
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AI reactions: generated {Count} pundit posts.", created);
    }
}
