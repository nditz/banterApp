using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.FootballReference.Jobs;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.Rss;
using BanterApp.Api.Integrations.SportsData;
using Hangfire;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Jobs;

public sealed record JobRunResult(
    int ItemsProcessed,
    int ItemsCreated,
    int ItemsUpdated,
    int ItemsSkipped,
    int ItemsFailed,
    string? ErrorMessage = null,
    object? Metadata = null);

public sealed record AdminJobDto(
    string JobKey,
    string DisplayName,
    string Description,
    string Status,
    string? Schedule,
    DateTimeOffset? LastRunAt,
    DateTimeOffset? NextRunAt,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastFailureAt,
    double? AverageDurationMs,
    int FailureCount,
    int SuccessCount,
    bool Enabled,
    bool Paused,
    bool CanRunManually,
    bool CanPause,
    bool IsStub);

public interface IJobRegistryService
{
    Task<IReadOnlyList<AdminJobDto>> ListJobsAsync(CancellationToken ct = default);
    Task<AdminJobDto?> GetJobAsync(string jobKey, CancellationToken ct = default);
    Task RunJobAsync(string jobKey, CancellationToken ct = default);
    Task PauseJobAsync(string jobKey, CancellationToken ct = default);
    Task ResumeJobAsync(string jobKey, CancellationToken ct = default);
    Task EnableJobAsync(string jobKey, CancellationToken ct = default);
    Task DisableJobAsync(string jobKey, CancellationToken ct = default);
    Task PauseAllAsync(CancellationToken ct = default);
    Task ResumeAllAsync(CancellationToken ct = default);
    Task RetryFailedItemsAsync(string jobKey, CancellationToken ct = default);
}

public sealed class JobRegistryService(
    AppDbContext db,
    IRecurringJobManager recurringJobs,
    IBackgroundJobClient backgroundJobs,
    IOptions<BackgroundJobsOptions> backgroundJobsOptions) : IJobRegistryService
{
    public async Task<IReadOnlyList<AdminJobDto>> ListJobsAsync(CancellationToken ct = default)
    {
        var states = await db.JobRegistryStates.AsNoTracking().ToListAsync(ct);
        var runs = await db.SyncRuns.AsNoTracking().ToListAsync(ct);
        var runningJobs = GetRunningHangfireJobIds();

        return JobRegistry.All
            .Select(def => MapJob(def, states, runs, runningJobs))
            .ToList();
    }

    public async Task<AdminJobDto?> GetJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = JobRegistry.FindByKey(jobKey);
        if (def is null)
        {
            return null;
        }

        var states = await db.JobRegistryStates.AsNoTracking().ToListAsync(ct);
        var runs = await db.SyncRuns.AsNoTracking()
            .Where(r => r.JobName == def.HangfireJobId)
            .ToListAsync(ct);
        var runningJobs = GetRunningHangfireJobIds();

        return MapJob(def, states, runs, runningJobs);
    }

    public async Task RunJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        EnsureRunnable(def, await GetStateAsync(def.Key, ct));

        if (def.IsStub)
        {
            backgroundJobs.Enqueue<StubMaintenanceJobs>(j => j.RunAsync(def.Key, CancellationToken.None));
            return;
        }

        recurringJobs.Trigger(def.HangfireJobId);
    }

    public async Task PauseJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        if (!def.CanPause)
        {
            throw new InvalidOperationException($"Job '{jobKey}' cannot be paused.");
        }

        var state = await GetOrCreateStateAsync(def, ct);
        state.Paused = true;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        recurringJobs.RemoveIfExists(def.HangfireJobId);
    }

    public async Task ResumeJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        var state = await GetOrCreateStateAsync(def, ct);
        state.Paused = false;
        state.Enabled = true;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!def.IsStub)
        {
            HangfireJobRegistration.RegisterSingleJob(recurringJobs, def.HangfireJobId, backgroundJobsOptions.Value);
        }
    }

    public async Task EnableJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        var state = await GetOrCreateStateAsync(def, ct);
        state.Enabled = true;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (!state.Paused && !def.IsStub)
        {
            HangfireJobRegistration.RegisterSingleJob(recurringJobs, def.HangfireJobId, backgroundJobsOptions.Value);
        }
    }

    public async Task DisableJobAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        var state = await GetOrCreateStateAsync(def, ct);
        state.Enabled = false;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        recurringJobs.RemoveIfExists(def.HangfireJobId);
    }

    public async Task PauseAllAsync(CancellationToken ct = default)
    {
        foreach (var def in JobRegistry.All.Where(j => j.CanPause))
        {
            await PauseJobAsync(def.Key, ct);
        }
    }

    public async Task ResumeAllAsync(CancellationToken ct = default)
    {
        foreach (var def in JobRegistry.All.Where(j => j.CanPause))
        {
            await ResumeJobAsync(def.Key, ct);
        }
    }

    public async Task RetryFailedItemsAsync(string jobKey, CancellationToken ct = default)
    {
        var def = RequireJob(jobKey);
        if (def.Key == "failed-items.retry")
        {
            backgroundJobs.Enqueue<StubMaintenanceJobs>(j => j.RetryFailedItemsAsync(CancellationToken.None));
            return;
        }

        var failedItems = await db.MediaItems
            .Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Failed)
            .Take(50)
            .ToListAsync(ct);

        foreach (var item in failedItems)
        {
            item.ProcessingStatus = MediaItemProcessingStatus.Pending;
            item.ProcessingError = null;
        }

        await db.SaveChangesAsync(ct);

        if (failedItems.Count > 0)
        {
            recurringJobs.Trigger(ContentEnrichmentJob.JobId);
        }
    }

    private static JobDefinition RequireJob(string jobKey) =>
        JobRegistry.FindByKey(jobKey)
        ?? throw new KeyNotFoundException($"Unknown job key '{jobKey}'.");

    private static void EnsureRunnable(JobDefinition def, JobRegistryState? state)
    {
        if (state is { Enabled: false })
        {
            throw new InvalidOperationException($"Job '{def.Key}' is disabled.");
        }
    }

    private async Task<JobRegistryState?> GetStateAsync(string jobKey, CancellationToken ct) =>
        await db.JobRegistryStates.FirstOrDefaultAsync(s => s.JobKey == jobKey, ct);

    private async Task<JobRegistryState> GetOrCreateStateAsync(JobDefinition def, CancellationToken ct)
    {
        var state = await db.JobRegistryStates.FirstOrDefaultAsync(s => s.JobKey == def.Key, ct);
        if (state is not null)
        {
            return state;
        }

        state = new JobRegistryState
        {
            Id = Guid.NewGuid(),
            JobKey = def.Key,
            Enabled = true,
            Paused = false,
            Schedule = def.DefaultSchedule,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.JobRegistryStates.Add(state);
        await db.SaveChangesAsync(ct);
        return state;
    }

    private static AdminJobDto MapJob(
        JobDefinition def,
        IReadOnlyList<JobRegistryState> states,
        IReadOnlyList<SyncRun> allRuns,
        HashSet<string> runningJobs)
    {
        var state = states.FirstOrDefault(s => s.JobKey == def.Key);
        var runs = allRuns.Where(r => r.JobName == def.HangfireJobId).ToList();
        var lastRun = runs.OrderByDescending(r => r.StartedAt).FirstOrDefault();
        var lastSuccess = runs.Where(r => r.Status == "completed").OrderByDescending(r => r.FinishedAt).FirstOrDefault();
        var lastFailure = runs.Where(r => r.Status == "failed").OrderByDescending(r => r.FinishedAt).FirstOrDefault();
        var completedRuns = runs.Where(r => r.FinishedAt.HasValue).ToList();

        var enabled = state?.Enabled ?? true;
        var paused = state?.Paused ?? false;
        var status = ResolveStatus(def, enabled, paused, runningJobs.Contains(def.HangfireJobId), lastRun);

        return new AdminJobDto(
            JobKey: def.Key,
            DisplayName: def.DisplayName,
            Description: def.Description,
            Status: status,
            Schedule: state?.Schedule ?? def.DefaultSchedule,
            LastRunAt: lastRun?.StartedAt,
            NextRunAt: null,
            LastSuccessAt: lastSuccess?.FinishedAt,
            LastFailureAt: lastFailure?.FinishedAt,
            AverageDurationMs: completedRuns.Count == 0
                ? null
                : completedRuns.Average(r => (r.FinishedAt!.Value - r.StartedAt).TotalMilliseconds),
            FailureCount: runs.Count(r => r.Status == "failed"),
            SuccessCount: runs.Count(r => r.Status == "completed"),
            Enabled: enabled,
            Paused: paused,
            CanRunManually: def.CanRunManually,
            CanPause: def.CanPause,
            IsStub: def.IsStub);
    }

    private static string ResolveStatus(
        JobDefinition def,
        bool enabled,
        bool paused,
        bool isRunning,
        SyncRun? lastRun)
    {
        if (!enabled)
        {
            return "disabled";
        }

        if (paused)
        {
            return "paused";
        }

        if (isRunning || lastRun?.Status == "running")
        {
            return "running";
        }

        if (lastRun?.Status == "failed")
        {
            return "failed";
        }

        return "idle";
    }

    private static HashSet<string> GetRunningHangfireJobIds()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var monitor = JobStorage.Current.GetMonitoringApi();
            var processing = monitor.ProcessingJobs(0, 50);
            foreach (var job in processing)
            {
                if (!string.IsNullOrWhiteSpace(job.Value?.Job?.Method?.DeclaringType?.Name))
                {
                    result.Add(MapTypeToJobId(job.Value.Job.Method.DeclaringType.Name));
                }
            }
        }
        catch
        {
            // Hangfire monitoring may be unavailable in tests.
        }

        return result;
    }

    private static string MapTypeToJobId(string typeName) => typeName switch
    {
        nameof(RssFeedResolveJob) => RssFeedResolveJob.JobId,
        nameof(RssOpinionSyncJob) => RssOpinionSyncJob.JobId,
        nameof(YouTubeSearchSyncJob) => YouTubeSearchSyncJob.JobId,
        nameof(MediaIngestJob) => MediaIngestJob.JobId,
        nameof(ContentEnrichmentJob) => ContentEnrichmentJob.JobId,
        nameof(PunditExtractionJob) => PunditExtractionJob.JobId,
        nameof(FeedBanterEnrichmentJob) => FeedBanterEnrichmentJob.JobId,
        nameof(PredictionAggregateJob) => PredictionAggregateJob.JobId,
        nameof(ScoreSyncJob) => ScoreSyncJob.JobId,
        nameof(MatchDetailsSyncJob) => MatchDetailsSyncJob.JobId,
        nameof(StandingsSyncJob) => StandingsSyncJob.JobId,
        nameof(NewsIngestJob) => NewsIngestJob.JobId,
        nameof(AiReactionJob) => AiReactionJob.JobId,
        nameof(FootballCountriesSyncJob) => FootballCountriesSyncJob.JobId,
        nameof(FootballPlayersSyncJob) => FootballPlayersSyncJob.JobId,
        nameof(FootballPlayerStatsSyncJob) => FootballPlayerStatsSyncJob.JobId,
        nameof(FootballTopScorersSyncJob) => FootballTopScorersSyncJob.JobId,
        nameof(FootballTopAssistsSyncJob) => FootballTopAssistsSyncJob.JobId,
        nameof(FootballReferenceFullSyncJob) => FootballReferenceFullSyncJob.JobId,
        nameof(StubMaintenanceJobs) => "failed-items-retry",
        _ => typeName
    };
}
