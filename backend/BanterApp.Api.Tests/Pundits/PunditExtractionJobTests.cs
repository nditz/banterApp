using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.Pundits.Dtos;
using BanterApp.Api.Services;
using BanterApp.Api.Tests.Infrastructure;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests.Pundits;

public class PunditExtractionJobTests
{
    [Fact]
    public async Task ExtractAsync_RateLimit_StopsBatchAndLeavesItemsEnriched()
    {
        await using var db = TestDbContextFactory.Create();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Sky",
            SourceType = "rss",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MediaSources.Add(source);
        db.MediaItems.AddRange(
            EnrichedItem(source.Id, "one"),
            EnrichedItem(source.Id, "two"));
        await db.SaveChangesAsync();

        var extractor = new RateLimitExtractor();
        var tracking = new RecordingErrorTracking();
        var usage = new RecordingUsageGuard();
        var job = new PunditExtractionJob(
            db,
            extractor,
            persistence: null!,
            Options.Create(new PunditIngestOptions { Enabled = true, ExtractionBatchSize = 5, MinSourceTextLength = 20 }),
            new SyncRunTracker(db, tracking, NullLogger<SyncRunTracker>.Instance),
            new StubRecurringJobs(),
            usage,
            tracking,
            NullLogger<PunditExtractionJob>.Instance);

        await job.ExtractAsync(CancellationToken.None);

        Assert.Equal(1, extractor.Calls);
        Assert.All(db.MediaItems, item => Assert.Equal(MediaItemProcessingStatus.Enriched, item.ProcessingStatus));
        Assert.Contains(tracking.Requests, r => r.ErrorCode == ErrorCodes.RateLimited);
        Assert.DoesNotContain(tracking.Requests, r => r.ErrorCode == ErrorCodes.JobFailed);
        Assert.True(usage.CircuitOpened);
    }

    [Fact]
    public void IsOpenAiRateLimit_DetectsMapped429()
    {
        var ex = ProviderErrorMapper.MapOpenAi(429, "opinion.extract");
        Assert.True(PunditExtractionJob.IsOpenAiRateLimit(ex));
        Assert.False(PunditExtractionJob.IsOpenAiRateLimit(new InvalidOperationException("boom")));
    }

    private static MediaItem EnrichedItem(Guid sourceId, string suffix) => new()
    {
        Id = Guid.NewGuid(),
        MediaSourceId = sourceId,
        ExternalId = $"ext-{suffix}",
        Title = $"Title {suffix}",
        Description = "desc",
        SourceUrl = $"https://example.com/{suffix}",
        RawText = "Premier League pundits discussed the title race in detail during a long studio segment that is not just the title.",
        ProcessingStatus = MediaItemProcessingStatus.Enriched,
        LastSyncedAt = DateTimeOffset.UtcNow
    };

    private sealed class RateLimitExtractor : IPunditOpinionExtractor
    {
        public int Calls { get; private set; }

        public Task<PunditExtractionResult?> ExtractAsync(
            string sourceType,
            string sourceName,
            string sourceUrl,
            string sourceTitle,
            DateTimeOffset? publishedAt,
            string? author,
            string sourceText,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            throw ProviderErrorMapper.MapOpenAi(429, "opinion.extract");
        }
    }

    private sealed class RecordingUsageGuard : IProviderUsageGuard
    {
        public bool CircuitOpened { get; private set; }

        public Task<bool> CanInvokeAsync(string provider, int estimatedUnits = 1, CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task RecordSuccessAsync(string provider, int estimatedUnits = 1, int latencyMs = 0, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordFailureAsync(string provider, string message, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<ProviderUsageSummary> GetTodaySummaryAsync(string provider, CancellationToken ct = default) =>
            Task.FromResult(new ProviderUsageSummary(provider, 0, 0, 0, null, CircuitOpened));

        public bool IsCircuitOpen(string provider) => CircuitOpened;

        public void OpenCircuit(string provider) => CircuitOpened = true;
    }

    private sealed class RecordingErrorTracking : IErrorTrackingService
    {
        public List<ErrorTrackRequest> Requests { get; } = [];

        public Task<Guid> TrackAsync(ErrorTrackRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(Guid.NewGuid());
        }

        public Task<Guid> TrackExceptionAsync(ErrorTrackRequest request, Exception exception, CancellationToken ct = default) =>
            TrackAsync(request, ct);
    }

    private sealed class StubRecurringJobs : IRecurringJobManager
    {
        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options)
        {
        }

        public void RemoveIfExists(string recurringJobId)
        {
        }

        public void Trigger(string recurringJobId)
        {
        }
    }
}
