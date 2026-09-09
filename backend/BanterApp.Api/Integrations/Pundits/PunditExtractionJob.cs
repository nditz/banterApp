using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditExtractionJob
{
    public const string JobId = "pundit-extraction";
    private const string Provider = "pundit-extraction";
    private const string OpenAiProvider = "openai";

    private readonly AppDbContext _db;
    private readonly IPunditOpinionExtractor _extractor;
    private readonly PunditOpinionPersistenceService _persistence;
    private readonly PunditIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IProviderUsageGuard _usage;
    private readonly IErrorTrackingService _errorTracking;
    private readonly ILogger<PunditExtractionJob> _logger;

    public PunditExtractionJob(
        AppDbContext db,
        IPunditOpinionExtractor extractor,
        PunditOpinionPersistenceService persistence,
        IOptions<PunditIngestOptions> options,
        SyncRunTracker tracker,
        IRecurringJobManager recurringJobs,
        IProviderUsageGuard usage,
        IErrorTrackingService errorTracking,
        ILogger<PunditExtractionJob> logger)
    {
        _db = db;
        _extractor = extractor;
        _persistence = persistence;
        _options = options.Value;
        _tracker = tracker;
        _recurringJobs = recurringJobs;
        _usage = usage;
        _errorTracking = errorTracking;
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
            if (!await _usage.CanInvokeAsync(OpenAiProvider, estimatedUnits: 1, cancellationToken))
            {
                _logger.LogWarning("Skipping pundit extraction; OpenAI usage guard is blocking calls.");
                await _tracker.CompleteAsync(run, extracted, 0, failed, ct: cancellationToken);
                return;
            }

            var batchSize = Math.Clamp(_options.ExtractionBatchSize, 1, 20);
            var items = await _db.MediaItems
                .Include(i => i.MediaSource)
                .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Enriched &&
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

                var sourceText = item.RawText;
                if (!SourceTextQuality.IsUsable(sourceText, _options.MinSourceTextLength) ||
                    SourceTextQuality.IsTitleDescriptionFallback(item.Title, item.Description, sourceText))
                {
                    item.ProcessingStatus = MediaItemProcessingStatus.Skipped;
                    item.ProcessingError = "Source text is not a usable transcript or article body.";
                    continue;
                }

                try
                {
                    var sourceType = MapSourceType(item.MediaSource.SourceType);
                    var extraction = await _extractor.ExtractAsync(
                        sourceType,
                        item.Publication ?? item.MediaSource.Name,
                        item.SourceUrl,
                        item.Title,
                        item.PublishedAt,
                        item.Author,
                        sourceText!,
                        cancellationToken);

                    if (extraction is null || extraction.Pundits.Count == 0)
                    {
                        item.ProcessingStatus = MediaItemProcessingStatus.Skipped;
                        item.ProcessingError = "No grounded pundit opinions in source text.";
                        continue;
                    }

                    var count = await _persistence.PersistExtractionAsync(item, extraction, cancellationToken);
                    extracted += count;
                    await _usage.RecordSuccessAsync(OpenAiProvider, estimatedUnits: 1, ct: cancellationToken);
                }
                catch (Exception ex) when (IsOpenAiRateLimit(ex))
                {
                    _logger.LogWarning(ex, "OpenAI rate-limited pundit extraction; stopping this batch.");
                    await _usage.RecordFailureAsync(OpenAiProvider, ex.Message, cancellationToken);
                    _usage.OpenCircuit(OpenAiProvider);
                    await _errorTracking.TrackAsync(new ErrorTrackRequest
                    {
                        Source = "job",
                        ErrorCode = ErrorCodes.RateLimited,
                        MessageSafe = "OpenAI pundit extraction is rate-limited; remaining items will retry later.",
                        Severity = "warning",
                        JobKey = "openai.opinion.extract",
                        JobRunId = run.Id,
                        Provider = OpenAiProvider,
                        IsRetryable = true,
                        Metadata = new Dictionary<string, object?>
                        {
                            ["media_item_id"] = item.Id
                        }
                    }, cancellationToken);
                    break;
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
                    await _usage.RecordFailureAsync(OpenAiProvider, ex.Message, cancellationToken);
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

    public static bool IsOpenAiRateLimit(Exception ex) =>
        ex is ProviderAppException provider &&
        (provider.Code == ErrorCodes.RateLimited || provider.StatusCode == StatusCodes.Status429TooManyRequests);

    private static string MapSourceType(string sourceType) =>
        sourceType switch
        {
            "youtube" => "youtube",
            "rss" => "rss",
            _ => "article"
        };
}
