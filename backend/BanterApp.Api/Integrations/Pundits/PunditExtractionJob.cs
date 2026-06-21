using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditExtractionJob
{
    public const string JobId = "pundit-extraction";
    private const string Provider = "pundit-extraction";

    private readonly AppDbContext _db;
    private readonly IPunditOpinionExtractor _extractor;
    private readonly PunditOpinionPersistenceService _persistence;
    private readonly PunditIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly ILogger<PunditExtractionJob> _logger;

    public PunditExtractionJob(
        AppDbContext db,
        IPunditOpinionExtractor extractor,
        PunditOpinionPersistenceService persistence,
        IOptions<PunditIngestOptions> options,
        SyncRunTracker tracker,
        IRecurringJobManager recurringJobs,
        ILogger<PunditExtractionJob> logger)
    {
        _db = db;
        _extractor = extractor;
        _persistence = persistence;
        _options = options.Value;
        _tracker = tracker;
        _recurringJobs = recurringJobs;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ExtractAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var extracted = 0;
        var failed = 0;

        try
        {
            var batchSize = Math.Clamp(_options.ExtractionBatchSize, 1, 20);
            var items = await _db.MediaItems
                .Include(i => i.MediaSource)
                .Where(i => (i.ProcessingStatus == MediaItemProcessingStatus.Enriched ||
                             i.ProcessingStatus == MediaItemProcessingStatus.Failed) &&
                            i.MediaSource.ExtractPredictions &&
                            i.MediaSource.IsActive)
                .OrderBy(i => i.LastSyncedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                if (await _db.PunditOpinions.AnyAsync(o => o.SourceItemId == item.Id, cancellationToken))
                {
                    item.ProcessingStatus = MediaItemProcessingStatus.Extracted;
                    item.ProcessedAt = DateTimeOffset.UtcNow;
                    continue;
                }

                try
                {
                    var sourceType = MapSourceType(item.MediaSource.SourceType);
                    var sourceText = item.RawText ?? item.Description ?? item.Title;
                    var extraction = await _extractor.ExtractAsync(
                        sourceType,
                        item.Publication ?? item.MediaSource.Name,
                        item.SourceUrl,
                        item.Title,
                        item.PublishedAt,
                        item.Author,
                        sourceText,
                        cancellationToken);

                    if (extraction is null || extraction.Pundits.Count == 0)
                    {
                        item.ProcessingStatus = MediaItemProcessingStatus.Failed;
                        item.ProcessingError = "Extraction returned no pundit opinions.";
                        failed++;
                        continue;
                    }

                    var count = await _persistence.PersistExtractionAsync(item, extraction, cancellationToken);
                    extracted += count;
                }
                catch (Exception ex)
                {
                    failed++;
                    item.ProcessingStatus = MediaItemProcessingStatus.Failed;
                    item.ProcessingError = StringLimits.Truncate(ex.Message, StringLimits.ProcessingError);
                    _logger.LogWarning(ex, "Pundit extraction failed for item {ItemId}.", item.Id);
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
            await _tracker.CompleteAsync(run, extracted, 0, failed, ct: cancellationToken);

            if (extracted > 0)
            {
                _recurringJobs.Trigger(PredictionAggregateJob.JobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pundit extraction job failed.");
            await _tracker.FailAsync(run, extracted, 0, ex, cancellationToken);
            throw;
        }
    }

    private static string MapSourceType(string sourceType) =>
        sourceType switch
        {
            "youtube" => "youtube",
            "rss" => "rss",
            _ => "article"
        };
}
