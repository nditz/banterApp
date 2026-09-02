using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.UserPredictions;
using BanterApp.Api.Integrations.Banter;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.FootballBanter;
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

    private static readonly HashSet<string> MatchCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "match_live",
        "match_result",
        "match_fixture",
    };

    private readonly AppDbContext _db;
    private readonly IFootballBanterEngine _banterEngine;
    private readonly BanterContextEnricher _banterContext;
    private readonly IBanterGenerator _banterGenerator;
    private readonly FeedReactionMediaService _feedReactionMedia;
    private readonly FeedRelevanceScorer _relevanceScorer;
    private readonly AiOptions _aiOptions;
    private readonly BackgroundJobsOptions _jobOptions;
    private readonly ProcessingOptions _processing;
    private readonly IApplicationErrorLogger _errorLogger;
    private readonly ILogger<FeedBanterEnrichmentJob> _logger;

    public FeedBanterEnrichmentJob(
        AppDbContext db,
        IFootballBanterEngine banterEngine,
        BanterContextEnricher banterContext,
        IBanterGenerator banterGenerator,
        FeedReactionMediaService feedReactionMedia,
        FeedRelevanceScorer relevanceScorer,
        IOptions<AiOptions> aiOptions,
        IOptions<BackgroundJobsOptions> jobOptions,
        IOptions<ProcessingOptions> processing,
        IApplicationErrorLogger errorLogger,
        ILogger<FeedBanterEnrichmentJob> logger)
    {
        _db = db;
        _banterEngine = banterEngine;
        _banterContext = banterContext;
        _banterGenerator = banterGenerator;
        _feedReactionMedia = feedReactionMedia;
        _relevanceScorer = relevanceScorer;
        _aiOptions = aiOptions.Value;
        _jobOptions = jobOptions.Value;
        _processing = processing.Value;
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
            .OrderByDescending(n => n.QualityScore ?? 0)
            .ThenByDescending(n => n.PublishedAt)
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

            var linkedOpinion = await TryLoadLinkedOpinionAsync(item, cancellationToken);
            var relevance = _relevanceScorer.Score(item, linkedOpinion);
            item.QualityScore ??= relevance.Score;

            if (!_relevanceScorer.ShouldGenerateBanter(relevance))
            {
                continue;
            }

            var headline = FeedBanterFormat.Strip(item.Title);
            var summary = FeedBanterFormat.Strip(item.Summary ?? item.Title);
            var referenceContext = await _banterContext.BuildContextJsonAsync(cancellationToken);
            var sourceText = summary;

            if (item.Category is not null &&
                MatchCategories.Contains(item.Category) &&
                !string.IsNullOrWhiteSpace(item.MatchId))
            {
                var punditContext = await MatchFeedContextBuilder.BuildPunditContextAsync(
                    _db,
                    item.MatchId,
                    cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(punditContext))
                {
                    sourceText = $"{summary}\n\nPundit context: {punditContext}";
                    item.PredictionSummary ??= punditContext;
                }
            }

            var output = await _banterEngine.GenerateAsync(
                new FootballBanterSourceInput
                {
                    SourceType = MapSourceType(item.Category),
                    SourceName = item.Source,
                    SourceUrl = item.Url!,
                    SourceTitle = headline,
                    PublishedAt = item.PublishedAt,
                    PunditName = item.Author,
                    SourceText = sourceText,
                    Prediction = linkedOpinion?.Prediction ?? item.PredictionSummary,
                    Confidence = linkedOpinion?.Confidence,
                    StatementType = ResolveStatementType(linkedOpinion),
                    ReferenceContextJson = referenceContext
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
            var textQueries = FeedReactionMediaService.BuildSearchQueries(
                output.Headline,
                output.BanterSummary,
                output.PunditName,
                item.Category);
            var banterContext = BanterContextFactory.FromFeedItem(
                item.MatchId,
                output.Headline,
                output.BanterSummary,
                item.Category,
                mood);
            var media = await _banterGenerator.GenerateAsync(
                BanterContextFactory.CreateRequest(
                    banterContext,
                    output.GifSuggestions.Concat(textQueries),
                    mood,
                    item.Id.GetHashCode()),
                cancellationToken);
            item.ImageUrl = media.Url;
            item.MediaType = media.MediaType;
            item.QualityScore = Math.Max(item.QualityScore ?? 0, relevance.Score);
            processed++;
        }

        if (processed > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        var stickerCandidates = await _db.NewsFeedItems
            .Where(n => n.ImageUrl != null && n.ImageUrl.StartsWith("/reactions/"))
            .OrderByDescending(n => n.PublishedAt)
            .Take(Math.Clamp(_jobOptions.FeedBanterEnrichmentBatchSize, 1, 25))
            .ToListAsync(cancellationToken);

        var upgraded = await _feedReactionMedia.UpgradeStoredStickersAsync(stickerCandidates, cancellationToken);
        if (upgraded > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Feed banter enrichment: rewrote {Count} feed cards; upgraded {Upgraded} sticker rows to live GIFs.",
            processed,
            upgraded);
    }

    private async Task<PunditOpinion?> TryLoadLinkedOpinionAsync(
        NewsFeedItem item,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(item.Category, PunditOpinionFeedMapper.FeedCategory, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!item.Id.StartsWith(PunditOpinionFeedMapper.FeedItemIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = item.Id[PunditOpinionFeedMapper.FeedItemIdPrefix.Length..];
        if (!Guid.TryParse(suffix, out var opinionId))
        {
            return null;
        }

        return await _db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Match)
            .FirstOrDefaultAsync(o => o.Id == opinionId, cancellationToken);
    }

    private static FootballBanterStatementType ResolveStatementType(PunditOpinion? opinion)
    {
        if (opinion is null)
        {
            return FootballBanterStatementType.AiSummary;
        }

        if (opinion.IsDirectQuote)
        {
            return FootballBanterStatementType.DirectQuote;
        }

        if (!string.IsNullOrWhiteSpace(opinion.Prediction))
        {
            return FootballBanterStatementType.InferredPrediction;
        }

        return FootballBanterStatementType.Paraphrase;
    }

    private static string BuildFeedBody(FootballBanterOutput output)
    {
        var parts = new List<string> { output.BanterSummary.Trim() };

        if (output.MemeReactions.Count > 0)
        {
            parts.Add(string.Join("\n", output.MemeReactions));
        }
        else if (!string.IsNullOrWhiteSpace(output.Headline))
        {
            parts.Add($"POV: {output.Headline.Trim()} 💀");
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
