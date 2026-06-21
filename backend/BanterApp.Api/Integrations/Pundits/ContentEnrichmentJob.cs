using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class ContentEnrichmentJob
{
    public const string JobId = "pundit-content-enrich";
    private const string Provider = "pundit-enrich";

    private readonly AppDbContext _db;
    private readonly IArticleContentFetcher _articleFetcher;
    private readonly IYouTubeTranscriptProvider _transcriptProvider;
    private readonly PunditIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<ContentEnrichmentJob> _logger;

    public ContentEnrichmentJob(
        AppDbContext db,
        IArticleContentFetcher articleFetcher,
        IYouTubeTranscriptProvider transcriptProvider,
        IOptions<PunditIngestOptions> options,
        SyncRunTracker tracker,
        ILogger<ContentEnrichmentJob> logger)
    {
        _db = db;
        _articleFetcher = articleFetcher;
        _transcriptProvider = transcriptProvider;
        _options = options.Value;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task EnrichAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var enriched = 0;
        var failed = 0;

        try
        {
            var batchSize = Math.Clamp(_options.ExtractionBatchSize * 2, 1, 20);
            var items = await _db.MediaItems
                .Include(i => i.MediaSource)
                .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Pending &&
                            i.MediaSource.ExtractPredictions &&
                            i.MediaSource.IsActive)
                .OrderBy(i => i.LastSyncedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                try
                {
                    await EnrichItemAsync(item, cancellationToken);
                    enriched++;
                }
                catch (Exception ex)
                {
                    failed++;
                    item.ProcessingStatus = MediaItemProcessingStatus.Failed;
                    item.ProcessingError = StringLimits.Truncate(ex.Message, StringLimits.ProcessingError);
                    _logger.LogWarning(ex, "Content enrichment failed for item {ItemId}.", item.Id);
                    await _tracker.LogErrorAsync(
                        Provider,
                        JobId,
                        "media_item",
                        ex.Message,
                        run.Id,
                        item.ExternalId,
                        cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await _tracker.CompleteAsync(run, enriched, 0, failed, ct: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content enrichment job failed.");
            await _tracker.FailAsync(run, enriched, 0, ex, cancellationToken);
        }
    }

    private async Task EnrichItemAsync(MediaItem item, CancellationToken cancellationToken)
    {
        var sourceType = item.MediaSource.SourceType;

        if (string.Equals(sourceType, "youtube", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = item.ExternalId;
            var transcript = await _transcriptProvider.GetTranscriptAsync(
                videoId,
                item.Title,
                item.Description,
                cancellationToken);

            item.RawText = transcript.TranscriptText ?? transcript.FallbackText;
            item.TranscriptSnippet = StringLimits.Truncate(item.RawText, 280);
            item.ProcessingStatus = MediaItemProcessingStatus.Enriched;
            if (!transcript.IsComplete)
            {
                item.ProcessingError = "Transcript incomplete; using title/description fallback.";
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(item.RawText) && item.RawText.Length >= _options.MinSourceTextLength)
        {
            item.ProcessingStatus = MediaItemProcessingStatus.Enriched;
            return;
        }

        if (_options.FetchArticleBodies && !string.IsNullOrWhiteSpace(item.SourceUrl))
        {
            var body = await _articleFetcher.FetchArticleTextAsync(item.SourceUrl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                item.RawText = body;
                item.TranscriptSnippet = StringLimits.Truncate(body, 280);
            }
        }

        item.RawText ??= item.Description ?? item.RawSummary ?? item.Title;
        item.ProcessingStatus = string.IsNullOrWhiteSpace(item.RawText)
            ? MediaItemProcessingStatus.Skipped
            : MediaItemProcessingStatus.Enriched;
    }
}
