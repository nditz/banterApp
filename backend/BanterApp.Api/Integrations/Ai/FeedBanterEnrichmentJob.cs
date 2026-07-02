using BanterApp.Api.Data;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// Rewrites raw RSS, match desk, and pundit feed items into Gen Z banter voice with GIF moods.
/// </summary>
public sealed class FeedBanterEnrichmentJob
{
    public const string JobId = "feed-banter-enrich";

    private static readonly HashSet<string> SkippedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "ai_reaction",
        "banter",
        "meme",
    };

    private readonly AppDbContext _db;
    private readonly IFootballBanterEngine _banterEngine;
    private readonly ReactionMediaResolver _reactionMedia;
    private readonly AiOptions _aiOptions;
    private readonly BackgroundJobsOptions _jobOptions;
    private readonly IApplicationErrorLogger _errorLogger;
    private readonly ILogger<FeedBanterEnrichmentJob> _logger;

    public FeedBanterEnrichmentJob(
        AppDbContext db,
        IFootballBanterEngine banterEngine,
        ReactionMediaResolver reactionMedia,
        IOptions<AiOptions> aiOptions,
        IOptions<BackgroundJobsOptions> jobOptions,
        IApplicationErrorLogger errorLogger,
        ILogger<FeedBanterEnrichmentJob> logger)
    {
        _db = db;
        _banterEngine = banterEngine;
        _reactionMedia = reactionMedia;
        _aiOptions = aiOptions.Value;
        _jobOptions = jobOptions.Value;
        _errorLogger = errorLogger;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task EnrichAsync(CancellationToken cancellationToken)
    {
        if (!_aiOptions.Enabled)
        {
            return;
        }

        try
        {
            await EnrichCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Feed banter enrichment job failed.");
            await _errorLogger.LogExceptionAsync("background", ex, category: JobId, ct: cancellationToken);
        }
    }

    private async Task EnrichCoreAsync(CancellationToken cancellationToken)
    {
        var batchSize = Math.Clamp(_jobOptions.FeedBanterEnrichmentBatchSize, 1, 25);
        var candidates = await _db.NewsFeedItems
            .Where(n => n.Category == null ||
                        (n.Category != "ai_reaction" && n.Category != "banter" && n.Category != "meme"))
            .OrderByDescending(n => n.PublishedAt)
            .Take(batchSize * 4)
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var item in candidates)
        {
            if (processed >= batchSize)
            {
                break;
            }

            if (FeedBanterFormat.IsBanterized(item.Summary) || FeedBanterFormat.IsBanterized(item.Title))
            {
                continue;
            }

            if (item.Category is not null && SkippedCategories.Contains(item.Category))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Source) || string.IsNullOrWhiteSpace(item.Url))
            {
                continue;
            }

            var headline = FeedBanterFormat.Strip(item.Title);
            var summary = FeedBanterFormat.Strip(item.Summary ?? item.Title);

            var output = await _banterEngine.GenerateAsync(
                new FootballBanterSourceInput
                {
                    SourceType = MapSourceType(item.Category),
                    SourceName = item.Source,
                    SourceUrl = item.Url!,
                    SourceTitle = headline,
                    PublishedAt = item.PublishedAt,
                    PunditName = item.Author,
                    SourceText = summary,
                    StatementType = FootballBanterStatementType.AiSummary
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(output.SourceUrl) || string.IsNullOrWhiteSpace(output.SourceName))
            {
                continue;
            }

            if (output.NeedsHumanReview)
            {
                _logger.LogInformation(
                    "Skipping feed banter for item {ItemId} — flagged for human review.",
                    item.Id);
                continue;
            }

            var body = BuildFeedBody(output);
            item.Title = FeedBanterFormat.Mark(output.Headline);
            item.Summary = FeedBanterFormat.Mark(body);

            var mood = FootballBanterGifMoodResolver.Resolve(
                output.GifSuggestions,
                ResolveFallbackMood(item.Category));
            var media = await _reactionMedia.ResolveAsync(
                output.GifSuggestions,
                mood,
                item.Id.GetHashCode(),
                cancellationToken);
            item.ImageUrl = media.Url;
            item.MediaType = media.Type;
            processed++;
        }

        if (processed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Feed banter enrichment: rewrote {Count} feed cards.", processed);
    }

    private static string BuildFeedBody(FootballBanterOutput output)
    {
        var parts = new List<string> { output.BanterSummary.Trim() };

        if (output.MemeReactions.Count > 0)
        {
            parts.Add(string.Join("\n", output.MemeReactions));
        }

        if (output.FanReactions.Count > 0)
        {
            parts.Add(string.Join(" · ", output.FanReactions));
        }

        parts.Add(
            $"[{FootballBanterOutputParser.ToJsonString(output.StatementType)} · via {output.SourceName}]");

        return string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string MapSourceType(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "pundit_quote" => "rss",
            "match_live" or "match_result" or "match_fixture" => "article",
            _ => "rss"
        };

    private static string ResolveFallbackMood(string? category) =>
        category?.Trim().ToLowerInvariant() switch
        {
            "pundit_quote" => "pundit",
            "match_live" => "hype",
            "match_result" => "celebrate",
            "match_fixture" => "debate",
            _ => "news"
        };
}
