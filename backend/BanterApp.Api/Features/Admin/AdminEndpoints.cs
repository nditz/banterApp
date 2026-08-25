using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Jobs;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Pundits;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("Admin");

        group.MapGet("/overview", async (AdminOverviewService overview, CancellationToken ct) =>
            Results.Ok(await overview.GetOverviewAsync(ct)));

        group.MapGet("/jobs", async (IJobRegistryService jobs, CancellationToken ct) =>
            Results.Ok(await jobs.ListJobsAsync(ct)));

        group.MapPost("/jobs/{jobKey}/run", RunJob).RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/jobs/{jobKey}/pause", PauseJob).RequireRateLimiting(RateLimitPolicies.AdminJobsPauseResume);
        group.MapPost("/jobs/{jobKey}/resume", ResumeJob).RequireRateLimiting(RateLimitPolicies.AdminJobsPauseResume);
        group.MapPost("/jobs/{jobKey}/enable", EnableJob).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/jobs/{jobKey}/disable", DisableJob).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/jobs/{jobKey}/retry-failed", RetryFailedJob).RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/jobs/pause-all", PauseAllJobs).RequireRateLimiting(RateLimitPolicies.AdminJobsPauseResume);
        group.MapPost("/jobs/resume-all", ResumeAllJobs).RequireRateLimiting(RateLimitPolicies.AdminJobsPauseResume);

        group.MapGet("/jobs/{jobKey}/runs", GetJobRuns);
        group.MapGet("/jobs/{jobKey}/runs/{runId:guid}", GetJobRunDetail);

        group.MapGet("/errors", GetErrors);
        group.MapGet("/errors/{id:guid}", GetErrorDetail);
        group.MapPost("/errors/{id:guid}/investigate", InvestigateError).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/errors/{id:guid}/resolve", ResolveError).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/errors/{id:guid}/ignore", IgnoreError).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/errors/{id:guid}/retry", RetryError).RequireRateLimiting(RateLimitPolicies.AdminErrorsRetry);

        group.MapGet("/sources", GetSources);
        group.MapPost("/sources/{id:guid}/sync", SyncSource);
        group.MapPost("/sources/{id:guid}/enable", EnableSource);
        group.MapPost("/sources/{id:guid}/disable", DisableSource);

        group.MapGet("/source-items", GetSourceItems);
        group.MapPost("/source-items/{id:guid}/reprocess", ReprocessSourceItem);

        group.MapGet("/review", async (AdminReviewService review, CancellationToken ct) =>
            Results.Ok(await review.ListPendingAsync(ct)));
        group.MapPost("/review/{id:guid}/approve", ApproveReview).RequireRateLimiting(RateLimitPolicies.AdminReviewUpdate);
        group.MapPost("/review/{id:guid}/reject", RejectReview).RequireRateLimiting(RateLimitPolicies.AdminReviewUpdate);
        group.MapPost("/review/{id:guid}/update", UpdateReview).RequireRateLimiting(RateLimitPolicies.AdminReviewUpdate);

        group.MapGet("/stats", async (AdminOverviewService overview, CancellationToken ct) =>
            Results.Ok(await overview.GetStatsAsync(ct)));

        group.MapGet("/health", async (AdminHealthService health, CancellationToken ct) =>
            Results.Ok(await health.GetHealthAsync(ct)));

        group.MapGet("/launch-checklist", async (AdminHealthService health, CancellationToken ct) =>
            Results.Ok(await health.GetLaunchChecklistAsync(ct)));

        group.MapGet("/users", GetUsers);
        group.MapGet("/users/{userId:guid}", GetUserDetail);
        group.MapPost("/users/{userId:guid}/roles", GrantUserRole)
            .RequireRateLimiting(RateLimitPolicies.AdminUsersManage);
        group.MapDelete("/users/{userId:guid}/roles/{role}", RevokeUserRole)
            .RequireRateLimiting(RateLimitPolicies.AdminUsersManage);
        group.MapPost("/users/{userId:guid}/status", SetUserStatus)
            .RequireRateLimiting(RateLimitPolicies.AdminUsersManage);

        group.MapGet("/audit-logs", GetAuditLogs);

        group.MapPost("/backfill/rss", BackfillRss).RequireRateLimiting(RateLimitPolicies.RssSyncTrigger);
        group.MapPost("/backfill/youtube", BackfillYoutube).RequireRateLimiting(RateLimitPolicies.YoutubeSyncTrigger);
        group.MapPost("/backfill/failed-extractions", BackfillFailedExtractions).RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/backfill/prediction-aggregates", BackfillPredictionAggregates).RequireRateLimiting(RateLimitPolicies.Write);

        group.MapGet("/football-data/overview", GetFootballDataOverview);
        group.MapGet("/football-data/countries", GetFootballCountries);
        group.MapGet("/football-data/players", GetFootballPlayers);
        group.MapGet("/football-data/leaderboards", GetFootballLeaderboards);
        group.MapPatch("/football-data/countries/{id:guid}/active", SetCountryActive)
            .RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPatch("/football-data/players/{id:guid}/active", SetPlayerActive)
            .RequireRateLimiting(RateLimitPolicies.Write);
        group.MapPost("/football-data/sync/countries", SyncFootballCountries)
            .RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/football-data/sync/players", SyncFootballPlayers)
            .RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/football-data/sync/top-scorers", SyncFootballTopScorers)
            .RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/football-data/sync/top-assists", SyncFootballTopAssists)
            .RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
        group.MapPost("/football-data/sync/all", SyncAllFootballData)
            .RequireRateLimiting(RateLimitPolicies.AdminJobsRun);
    }

    private static async Task<IResult> RunJob(
        string jobKey,
        IJobRegistryService jobs,
        IUserContext user,
        IAdminAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        try
        {
            await jobs.RunJobAsync(jobKey, ct);
            await audit.LogAsync(user, http, "job.run", "job", jobKey, ct: ct);
            return Results.Ok(new { triggered = jobKey });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> PauseJob(
        string jobKey, IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.PauseJobAsync(jobKey, ct);
        await audit.LogAsync(user, http, "job.pause", "job", jobKey, ct: ct);
        return Results.Ok(new { paused = jobKey });
    }

    private static async Task<IResult> ResumeJob(
        string jobKey, IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.ResumeJobAsync(jobKey, ct);
        await audit.LogAsync(user, http, "job.resume", "job", jobKey, ct: ct);
        return Results.Ok(new { resumed = jobKey });
    }

    private static async Task<IResult> EnableJob(
        string jobKey, IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.EnableJobAsync(jobKey, ct);
        await audit.LogAsync(user, http, "job.enable", "job", jobKey, ct: ct);
        return Results.Ok(new { enabled = jobKey });
    }

    private static async Task<IResult> DisableJob(
        string jobKey, IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.DisableJobAsync(jobKey, ct);
        await audit.LogAsync(user, http, "job.disable", "job", jobKey, ct: ct);
        return Results.Ok(new { disabled = jobKey });
    }

    private static async Task<IResult> RetryFailedJob(
        string jobKey, IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.RetryFailedItemsAsync(jobKey, ct);
        await audit.LogAsync(user, http, "job.retry-failed", "job", jobKey, ct: ct);
        return Results.Ok(new { retried = jobKey });
    }

    private static async Task<IResult> PauseAllJobs(
        IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.PauseAllAsync(ct);
        await audit.LogAsync(user, http, "job.pause-all", "job", null, ct: ct);
        return Results.Ok(new { pausedAll = true });
    }

    private static async Task<IResult> ResumeAllJobs(
        IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.ResumeAllAsync(ct);
        await audit.LogAsync(user, http, "job.resume-all", "job", null, ct: ct);
        return Results.Ok(new { resumedAll = true });
    }

    private static async Task<IResult> GetJobRuns(
        string jobKey, AppDbContext db, int? limit, CancellationToken ct)
    {
        var def = JobRegistry.FindByKey(jobKey);
        if (def is null)
        {
            return Results.NotFound(new { error = "Unknown job key." });
        }

        var take = Math.Clamp(limit ?? 50, 1, 100);
        var runs = await db.SyncRuns.AsNoTracking()
            .Where(r => r.JobName == def.HangfireJobId)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .Select(r => new
            {
                runId = r.Id,
                jobKey,
                status = MapRunStatus(r.Status),
                r.StartedAt,
                r.FinishedAt,
                durationMs = r.DurationMs ?? (r.FinishedAt.HasValue
                    ? (long?)(r.FinishedAt.Value - r.StartedAt).TotalMilliseconds
                    : null),
                itemsProcessed = r.ItemsProcessed > 0 ? r.ItemsProcessed : r.RecordsCreated + r.RecordsUpdated + r.RecordsFailed,
                itemsCreated = r.RecordsCreated,
                itemsUpdated = r.RecordsUpdated,
                itemsSkipped = r.ItemsSkipped,
                itemsFailed = r.RecordsFailed,
                r.ErrorMessage,
                metadataJson = SecretSanitizer.SanitizeJson(r.MetadataJson)
            })
            .ToListAsync(ct);

        return Results.Ok(runs);
    }

    private static async Task<IResult> GetJobRunDetail(
        string jobKey, Guid runId, AppDbContext db, IOptions<AdminOptions> adminOptions, CancellationToken ct)
    {
        var def = JobRegistry.FindByKey(jobKey);
        if (def is null)
        {
            return Results.NotFound(new { error = "Unknown job key." });
        }

        var run = await db.SyncRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == runId, ct);
        if (run is null || run.JobName != def.HangfireJobId)
        {
            return Results.NotFound(new { error = "Run not found." });
        }

        var errors = await db.SyncErrors.AsNoTracking()
            .Where(e => e.SyncRunId == runId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(50)
            .Select(e => new { e.Id, e.EntityType, e.EntityId, e.Message, e.OccurredAt })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            runId = run.Id,
            jobKey,
            job = def,
            status = MapRunStatus(run.Status),
            run.StartedAt,
            run.FinishedAt,
            durationMs = run.DurationMs,
            itemsProcessed = run.ItemsProcessed,
            itemsCreated = run.RecordsCreated,
            itemsUpdated = run.RecordsUpdated,
            itemsSkipped = run.ItemsSkipped,
            itemsFailed = run.RecordsFailed,
            run.ErrorMessage,
            metadataJson = SecretSanitizer.SanitizeJson(run.MetadataJson),
            errors,
            detailAvailable = adminOptions.Value.ExposeErrorDetail
        });
    }

    private static async Task<IResult> GetErrors(
        AppDbContext db,
        string? status,
        string? severity,
        string? source,
        string? provider,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        CancellationToken ct)
    {
        var take = Math.Clamp(limit ?? 50, 1, 100);
        var query = db.OperationalErrors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(e => e.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(e => e.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            query = query.Where(e => e.Provider == provider);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.LastSeenAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.LastSeenAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.ErrorCode.Contains(term) ||
                (e.RequestId != null && e.RequestId.Contains(term)) ||
                e.MessageSafe.Contains(term));
        }

        var errors = await query
            .OrderByDescending(e => e.LastSeenAt)
            .Take(take)
            .Select(e => new
            {
                e.Id,
                e.Source,
                jobKey = e.JobKey,
                severity = e.Severity,
                message = e.MessageSafe,
                e.ErrorCode,
                e.Status,
                e.FirstSeenAt,
                e.LastSeenAt,
                count = e.OccurrenceCount,
                e.ResolvedAt,
                e.RequestId,
                jobRunId = e.JobRunId,
                sourceItemId = e.SourceItemId,
                e.Provider
            })
            .ToListAsync(ct);

        return Results.Ok(errors);
    }

    private static async Task<IResult> GetErrorDetail(
        Guid id,
        AppDbContext db,
        IOptions<AdminOptions> adminOptions,
        CancellationToken ct)
    {
        var error = await db.OperationalErrors.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (error is null)
        {
            return Results.NotFound();
        }

        var exposeDetail = adminOptions.Value.ExposeErrorDetail;
        return Results.Ok(new
        {
            error.Id,
            error.Fingerprint,
            error.RequestId,
            error.Source,
            error.Environment,
            error.Severity,
            error.Status,
            error.ErrorCode,
            error.ErrorType,
            message = error.MessageSafe,
            messageInternal = error.MessageInternal,
            stackTrace = exposeDetail ? error.StackTrace : null,
            error.Route,
            error.Method,
            error.StatusCode,
            error.UserId,
            error.AdminUserId,
            error.JobKey,
            jobRunId = error.JobRunId,
            sourceItemId = error.SourceItemId,
            error.Provider,
            error.ProviderRequestId,
            metadataJson = error.MetadataJson,
            error.FirstSeenAt,
            error.LastSeenAt,
            count = error.OccurrenceCount,
            error.ResolvedAt,
            error.CreatedAt,
            error.UpdatedAt,
            detailAvailable = exposeDetail
        });
    }

    private static async Task<IResult> InvestigateError(
        Guid id,
        AppDbContext db,
        IUserContext user,
        IAdminAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var error = await db.OperationalErrors.FindAsync([id], ct);
        if (error is null)
        {
            return Results.NotFound();
        }

        error.Status = "investigating";
        error.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "error.investigate", "operational_error", id.ToString(), ct: ct);
        return Results.Ok(new { investigating = id });
    }

    private static async Task<IResult> ResolveError(
        Guid id, AppDbContext db, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var error = await db.OperationalErrors.FindAsync([id], ct);
        if (error is null) return Results.NotFound();
        error.Status = "resolved";
        error.ResolvedAt = DateTimeOffset.UtcNow;
        error.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "error.resolve", "operational_error", id.ToString(), ct: ct);
        return Results.Ok(new { resolved = id });
    }

    private static async Task<IResult> IgnoreError(
        Guid id, AppDbContext db, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var error = await db.OperationalErrors.FindAsync([id], ct);
        if (error is null) return Results.NotFound();
        error.Status = "ignored";
        error.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "error.ignore", "operational_error", id.ToString(), ct: ct);
        return Results.Ok(new { ignored = id });
    }

    private static async Task<IResult> RetryError(
        Guid id, AppDbContext db, IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var error = await db.OperationalErrors.FindAsync([id], ct);
        if (error is null) return Results.NotFound();

        if (error.SourceItemId.HasValue)
        {
            var item = await db.MediaItems.FindAsync([error.SourceItemId.Value], ct);
            if (item is not null)
            {
                item.ProcessingStatus = MediaItemProcessingStatus.Pending;
                item.ProcessingError = null;
            }
        }

        var jobDefinition = error.JobKey is not null ? JobRegistry.FindByKey(error.JobKey) : null;
        if (jobDefinition is not null)
        {
            recurring.Trigger(jobDefinition.HangfireJobId);
        }
        else
        {
            recurring.Trigger(ContentEnrichmentJob.JobId);
        }

        error.Status = "retry_scheduled";
        error.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "error.retry", "operational_error", id.ToString(), ct: ct);
        return Results.Ok(new { retryScheduled = id });
    }

    private static async Task<IResult> GetSources(AppDbContext db, CancellationToken ct)
    {
        var sources = await db.MediaSources.AsNoTracking()
            .Select(s => new
            {
                sourceId = s.Id,
                type = s.SourceType,
                s.Name,
                url = s.RssUrl ?? s.SiteUrl,
                enabled = s.IsActive,
                lastSyncAt = s.UpdatedAt,
                lastSuccessAt = db.MediaItems
                    .Where(i => i.MediaSourceId == s.Id && i.ProcessingStatus != MediaItemProcessingStatus.Failed)
                    .Max(i => (DateTimeOffset?)i.LastSyncedAt),
                lastErrorAt = db.MediaItems
                    .Where(i => i.MediaSourceId == s.Id && i.ProcessingStatus == MediaItemProcessingStatus.Failed)
                    .Max(i => (DateTimeOffset?)i.ProcessedAt),
                itemsIngested = db.MediaItems.Count(i => i.MediaSourceId == s.Id),
                failureCount = db.MediaItems.Count(i => i.MediaSourceId == s.Id && i.ProcessingStatus == MediaItemProcessingStatus.Failed)
            })
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return Results.Ok(sources);
    }

    private static async Task<IResult> SyncSource(
        Guid id, AppDbContext db, IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var source = await db.MediaSources.FindAsync([id], ct);
        if (source is null) return Results.NotFound();

        var jobId = source.SourceType switch
        {
            "youtube" => YouTubeSearchSyncJob.JobId,
            "rss" => RssOpinionSyncJob.JobId,
            _ => MediaIngestJob.JobId
        };

        recurring.Trigger(jobId);
        await audit.LogAsync(user, http, "source.sync", "media_source", id.ToString(), ct: ct);
        return Results.Ok(new { triggered = jobId, sourceId = id });
    }

    private static async Task<IResult> EnableSource(
        Guid id, AppDbContext db, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var source = await db.MediaSources.FindAsync([id], ct);
        if (source is null) return Results.NotFound();
        source.IsActive = true;
        source.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "source.enable", "media_source", id.ToString(), ct: ct);
        return Results.Ok(new { enabled = id });
    }

    private static async Task<IResult> DisableSource(
        Guid id, AppDbContext db, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var source = await db.MediaSources.FindAsync([id], ct);
        if (source is null) return Results.NotFound();
        source.IsActive = false;
        source.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(user, http, "source.disable", "media_source", id.ToString(), ct: ct);
        return Results.Ok(new { disabled = id });
    }

    private static async Task<IResult> GetSourceItems(
        AppDbContext db,
        Guid? sourceId,
        string? status,
        bool? needsReview,
        bool? hasErrors,
        bool? missingTranscript,
        int? limit,
        CancellationToken ct)
    {
        var take = Math.Clamp(limit ?? 50, 1, 100);
        var query = db.MediaItems.AsNoTracking()
            .Include(i => i.MediaSource)
            .Include(i => i.Opinions)
            .AsQueryable();

        if (sourceId.HasValue) query = query.Where(i => i.MediaSourceId == sourceId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.ProcessingStatus == status);
        if (hasErrors == true) query = query.Where(i => i.ProcessingStatus == MediaItemProcessingStatus.Failed);
        if (missingTranscript == true) query = query.Where(i => i.RawText == null && i.TranscriptSnippet == null);
        if (needsReview == true) query = query.Where(i => i.Opinions.Any(o => o.NeedsHumanReview && o.ReviewStatus == "pending"));

        var items = await query
            .OrderByDescending(i => i.LastSyncedAt)
            .Take(take)
            .Select(i => new
            {
                i.Id,
                i.Title,
                sourceName = i.MediaSource.Name,
                sourceType = i.MediaSource.SourceType,
                publishedAt = i.PublishedAt,
                fetchedAt = i.LastSyncedAt,
                processedAt = i.ProcessedAt,
                status = i.ProcessingStatus,
                hasRawText = i.RawText != null || i.TranscriptSnippet != null,
                hasPredictions = i.Opinions.Any(o => o.Prediction != null),
                needsHumanReview = i.Opinions.Any(o => o.NeedsHumanReview && o.ReviewStatus == "pending"),
                processingError = i.ProcessingError
            })
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> ReprocessSourceItem(
        Guid id, AppDbContext db, IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var item = await db.MediaItems.FindAsync([id], ct);
        if (item is null) return Results.NotFound();
        item.ProcessingStatus = MediaItemProcessingStatus.Pending;
        item.ProcessingError = null;
        item.ProcessedAt = null;
        await db.SaveChangesAsync(ct);
        recurring.Trigger(ContentEnrichmentJob.JobId);
        await audit.LogAsync(user, http, "source-item.reprocess", "media_item", id.ToString(), ct: ct);
        return Results.Ok(new { reprocessed = id });
    }

    private static async Task<IResult> ApproveReview(
        Guid id, AdminReviewService review, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await review.ApproveAsync(id, user, ct);
        await audit.LogAsync(user, http, "review.approve", "pundit_opinion", id.ToString(), ct: ct);
        return Results.Ok(new { approved = id });
    }

    private static async Task<IResult> RejectReview(
        Guid id, AdminReviewRejectRequest request, AdminReviewService review, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await review.RejectAsync(id, user, request.Notes, ct);
        await audit.LogAsync(user, http, "review.reject", "pundit_opinion", id.ToString(), ct: ct);
        return Results.Ok(new { rejected = id });
    }

    private static async Task<IResult> UpdateReview(
        Guid id, AdminReviewUpdateRequest request, AdminReviewService review, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await review.UpdateAsync(id, request, user, ct);
        await audit.LogAsync(user, http, "review.update", "pundit_opinion", id.ToString(), ct: ct);
        return Results.Ok(new { updated = id });
    }

    private static async Task<IResult> GetUsers(
        AdminUsersService users,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        HttpContext http,
        int? page,
        int? pageSize,
        string? search,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.UsersView, ct))
        {
            return Results.Forbid();
        }

        return Results.Ok(await users.ListUsersAsync(page, pageSize, search, ct));
    }

    private static async Task<IResult> GetUserDetail(
        Guid userId,
        AdminUsersService users,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.UsersView, ct))
        {
            return Results.Forbid();
        }

        var detail = await users.GetUserAsync(userId, ct);
        return detail is null
            ? Results.NotFound(new { error = "User not found." })
            : Results.Ok(detail);
    }

    private static async Task<IResult> GrantUserRole(
        Guid userId,
        AdminUserRoleRequest request,
        AdminUsersService users,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        IAdminAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.UsersManage, ct))
        {
            return Results.Forbid();
        }

        if (!string.Equals(request.Role, AdminUsersService.AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = $"Unknown role '{request.Role}'." });
        }

        var result = await users.GrantAdminAsync(userId, ct);
        if (!result.Success)
        {
            return MapUserActionFailure(result);
        }

        await audit.LogAsync(user, http, "user.role.grant", "user", userId.ToString(),
            new { role = AdminUsersService.AdminRole }, ct);

        return Results.Ok(new { userId, role = AdminUsersService.AdminRole, message = result.Message });
    }

    private static async Task<IResult> RevokeUserRole(
        Guid userId,
        string role,
        AdminUsersService users,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        IAdminAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.UsersManage, ct))
        {
            return Results.Forbid();
        }

        if (!string.Equals(role, AdminUsersService.AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = $"Unknown role '{role}'." });
        }

        if (user.UserId is null)
        {
            return Results.Forbid();
        }

        var result = await users.RevokeAdminAsync(userId, user.UserId.Value, ct);
        if (!result.Success)
        {
            return MapUserActionFailure(result);
        }

        await audit.LogAsync(user, http, "user.role.revoke", "user", userId.ToString(),
            new { role = AdminUsersService.AdminRole }, ct);

        return Results.Ok(new { userId, role = AdminUsersService.AdminRole, message = result.Message });
    }

    private static async Task<IResult> SetUserStatus(
        Guid userId,
        AdminUserStatusRequest request,
        AdminUsersService users,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        IAdminAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.UsersManage, ct))
        {
            return Results.Forbid();
        }

        if (user.UserId is null)
        {
            return Results.Forbid();
        }

        var result = await users.SetStatusAsync(userId, request.Status ?? string.Empty, user.UserId.Value, ct);
        if (!result.Success)
        {
            return MapUserActionFailure(result);
        }

        await audit.LogAsync(user, http, "user.status.change", "user", userId.ToString(),
            new { status = request.Status }, ct);

        return Results.Ok(new { userId, status = request.Status, message = result.Message });
    }

    private static IResult MapUserActionFailure(AdminUserActionResult result) => result.Outcome switch
    {
        AdminUserActionOutcome.NotFound => Results.NotFound(new { error = result.Message }),
        AdminUserActionOutcome.Conflict => Results.Conflict(new { error = result.Message }),
        _ => Results.BadRequest(new { error = result.Message })
    };

    private static async Task<IResult> GetAuditLogs(
        AppDbContext db,
        IAdminAuthorizationService adminAuth,
        IUserContext user,
        HttpContext http,
        string? action,
        Guid? adminUserId,
        string? targetType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? page,
        int? pageSize,
        int? limit,
        CancellationToken ct)
    {
        if (!await adminAuth.HasPermissionAsync(user, http, AdminPermissions.AuditView, ct))
        {
            return Results.Forbid();
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            return Results.BadRequest(new { error = "'from' must not be after 'to'." });
        }

        var currentPage = Math.Max(page ?? 1, 1);
        // `limit` is the pre-existing parameter name; keep honouring it as a page size.
        var size = Math.Clamp(pageSize ?? limit ?? 50, 1, 100);

        var query = db.AdminAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var term = action.Trim();
            query = query.Where(l => l.Action == term);
        }

        if (adminUserId.HasValue)
        {
            query = query.Where(l => l.AdminUserId == adminUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var term = targetType.Trim();
            query = query.Where(l => l.TargetType == term);
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= to.Value);
        }

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(l => new
            {
                l.Id,
                l.AdminUserId,
                l.Action,
                l.TargetType,
                l.TargetId,
                metadataJson = SecretSanitizer.SanitizeJson(l.MetadataJson),
                l.IpAddress,
                l.CreatedAt
            })
            .ToListAsync(ct);

        var actions = await db.AdminAuditLogs.AsNoTracking()
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .Take(100)
            .ToListAsync(ct);

        return Results.Ok(new
        {
            items = logs,
            page = currentPage,
            pageSize = size,
            total,
            availableActions = actions
        });
    }

    private static async Task<IResult> BackfillRss(
        IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        recurring.Trigger(RssOpinionSyncJob.JobId);
        await audit.LogAsync(user, http, "backfill.rss", "backfill", null, ct: ct);
        return Results.Ok(new { triggered = RssOpinionSyncJob.JobId });
    }

    private static async Task<IResult> BackfillYoutube(
        IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        recurring.Trigger(YouTubeSearchSyncJob.JobId);
        await audit.LogAsync(user, http, "backfill.youtube", "backfill", null, ct: ct);
        return Results.Ok(new { triggered = YouTubeSearchSyncJob.JobId });
    }

    private static async Task<IResult> BackfillFailedExtractions(
        IJobRegistryService jobs, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        await jobs.RetryFailedItemsAsync("failed-items.retry", ct);
        await audit.LogAsync(user, http, "backfill.failed-extractions", "backfill", null, ct: ct);
        return Results.Ok(new { triggered = "failed-items.retry" });
    }

    private static async Task<IResult> BackfillPredictionAggregates(
        IRecurringJobManager recurring, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        recurring.Trigger(PredictionAggregateJob.JobId);
        await audit.LogAsync(user, http, "backfill.prediction-aggregates", "backfill", null, ct: ct);
        return Results.Ok(new { triggered = PredictionAggregateJob.JobId });
    }

    private static async Task<IResult> GetFootballDataOverview(
        FootballDataAdminService service, CancellationToken ct) =>
        Results.Ok(await service.GetOverviewAsync(ct));

    private static async Task<IResult> GetFootballCountries(
        FootballDataAdminService service, string? search, int? limit, CancellationToken ct) =>
        Results.Ok(await service.ListCountriesAsync(search, limit, ct));

    private static async Task<IResult> GetFootballPlayers(
        FootballDataAdminService service, Guid? countryId, string? position, string? search, int? limit, CancellationToken ct) =>
        Results.Ok(await service.ListPlayersAsync(countryId, position, search, limit, ct));

    private static async Task<IResult> GetFootballLeaderboards(
        FootballDataAdminService service, string? type, CancellationToken ct) =>
        Results.Ok(await service.ListLeaderboardsAsync(type, ct));

    private static async Task<IResult> SetCountryActive(
        Guid id, SetActiveRequest request, FootballDataAdminService service,
        IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var ok = await service.SetCountryActiveAsync(id, request.IsActive, ct);
        if (!ok) return Results.NotFound();
        await audit.LogAsync(user, http, "football.country.active", "country", id.ToString(),
            new { request.IsActive }, ct);
        return Results.Ok(new { id, request.IsActive });
    }

    private static async Task<IResult> SetPlayerActive(
        Guid id, SetActiveRequest request, FootballDataAdminService service,
        IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        var ok = await service.SetPlayerActiveAsync(id, request.IsActive, ct);
        if (!ok) return Results.NotFound();
        await audit.LogAsync(user, http, "football.player.active", "player", id.ToString(),
            new { request.IsActive }, ct);
        return Results.Ok(new { id, request.IsActive });
    }

    private static async Task<IResult> SyncFootballCountries(
        FootballDataAdminService service, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        service.TriggerSync("football.countries.sync");
        await audit.LogAsync(user, http, "football.sync.countries", "football-data", null, ct: ct);
        return Results.Ok(new { triggered = "football.countries.sync" });
    }

    private static async Task<IResult> SyncFootballPlayers(
        FootballDataAdminService service, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        service.TriggerSync("football.players.sync");
        await audit.LogAsync(user, http, "football.sync.players", "football-data", null, ct: ct);
        return Results.Ok(new { triggered = "football.players.sync" });
    }

    private static async Task<IResult> SyncFootballTopScorers(
        FootballDataAdminService service, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        service.TriggerSync("football.top_scorers.sync");
        await audit.LogAsync(user, http, "football.sync.top-scorers", "football-data", null, ct: ct);
        return Results.Ok(new { triggered = "football.top_scorers.sync" });
    }

    private static async Task<IResult> SyncFootballTopAssists(
        FootballDataAdminService service, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        service.TriggerSync("football.top_assists.sync");
        await audit.LogAsync(user, http, "football.sync.top-assists", "football-data", null, ct: ct);
        return Results.Ok(new { triggered = "football.top_assists.sync" });
    }

    private static async Task<IResult> SyncAllFootballData(
        FootballDataAdminService service, IUserContext user, IAdminAuditService audit, HttpContext http, CancellationToken ct)
    {
        service.TriggerSync("football.reference_data.full_sync");
        await audit.LogAsync(user, http, "football.sync.all", "football-data", null, ct: ct);
        return Results.Ok(new { triggered = "football.reference_data.full_sync" });
    }

    private static string MapRunStatus(string status) => status switch
    {
        "completed" => "success",
        "failed" => "failed",
        "running" => "running",
        _ => status
    };
}

public sealed record AdminReviewRejectRequest(string? Notes);

public sealed record SetActiveRequest(bool IsActive);

public sealed record AdminUserRoleRequest(string? Role);

public sealed record AdminUserStatusRequest(string? Status);
