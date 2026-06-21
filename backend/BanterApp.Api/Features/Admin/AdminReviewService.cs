using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Pundits;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Admin;

public sealed class AdminReviewService(
    AppDbContext db,
    IRecurringJobManager recurringJobs)
{
    public async Task<List<object>> ListPendingAsync(CancellationToken ct)
    {
        var items = await db.PunditOpinions
            .AsNoTracking()
            .Include(o => o.Pundit)
            .Include(o => o.SourceItem)
            .ThenInclude(i => i.MediaSource)
            .Where(o => o.NeedsHumanReview && o.ReviewStatus == "pending")
            .OrderByDescending(o => o.CreatedAt)
            .Take(100)
            .Select(o => new
            {
                o.Id,
                punditName = o.Pundit.Name,
                o.Opinion,
                o.Prediction,
                o.PredictionType,
                o.Confidence,
                o.EvidenceQuote,
                o.IsDirectQuote,
                o.NeedsHumanReview,
                o.ReviewStatus,
                sourceTitle = o.SourceItem.Title,
                sourceName = o.SourceItem.MediaSource.Name,
                sourceType = o.SourceItem.MediaSource.SourceType,
                o.CreatedAt
            })
            .ToListAsync(ct);

        return items.Cast<object>().ToList();
    }

    public async Task ApproveAsync(Guid id, IUserContext user, CancellationToken ct)
    {
        var opinion = await db.PunditOpinions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("Opinion not found.");

        opinion.NeedsHumanReview = false;
        opinion.ReviewStatus = "approved";
        opinion.ReviewedAt = DateTimeOffset.UtcNow;
        opinion.ReviewedByUserId = user.UserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid id, IUserContext user, string? notes, CancellationToken ct)
    {
        var opinion = await db.PunditOpinions.FindAsync([id], ct)
            ?? throw new KeyNotFoundException("Opinion not found.");

        opinion.ReviewStatus = "rejected";
        opinion.ReviewedAt = DateTimeOffset.UtcNow;
        opinion.ReviewedByUserId = user.UserId;
        opinion.ReviewNotes = notes;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Guid id, AdminReviewUpdateRequest request, IUserContext user, CancellationToken ct)
    {
        var opinion = await db.PunditOpinions
            .Include(o => o.Pundit)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException("Opinion not found.");

        if (!string.IsNullOrWhiteSpace(request.Opinion))
        {
            opinion.Opinion = request.Opinion;
        }

        if (request.Prediction is not null)
        {
            opinion.Prediction = request.Prediction;
        }

        if (request.PredictionType is not null)
        {
            opinion.PredictionType = request.PredictionType;
        }

        if (request.PunditName is not null)
        {
            opinion.Pundit.Name = request.PunditName;
        }

        if (request.IsDirectQuote.HasValue)
        {
            opinion.IsDirectQuote = request.IsDirectQuote.Value;
        }

        if (request.QuoteContext is not null)
        {
            opinion.QuoteContext = request.QuoteContext;
        }

        if (request.ReviewNotes is not null)
        {
            opinion.ReviewNotes = request.ReviewNotes;
        }

        opinion.ReviewedAt = DateTimeOffset.UtcNow;
        opinion.ReviewedByUserId = user.UserId;
        await db.SaveChangesAsync(ct);
    }

    public Task RerunExtractionAsync(Guid sourceItemId, CancellationToken ct)
    {
        recurringJobs.Trigger(PunditExtractionJob.JobId);
        return Task.CompletedTask;
    }
}

public sealed record AdminReviewUpdateRequest(
    string? Opinion,
    string? Prediction,
    string? PredictionType,
    string? PunditName,
    bool? IsDirectQuote,
    string? QuoteContext,
    string? ReviewNotes);
