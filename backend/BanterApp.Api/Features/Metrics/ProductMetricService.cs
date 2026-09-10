using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Metrics;

/// <summary>
/// Writes and reads <see cref="AppMetric"/> counters. Events are stored without any user
/// identifier — these are product counters, not per-person tracking.
/// </summary>
public sealed class ProductMetricService(AppDbContext db, ILogger<ProductMetricService> logger)
{
    public async Task RecordAsync(
        string metricKey,
        double value = 1,
        string? dimensionsJson = null,
        CancellationToken ct = default)
    {
        if (!ProductMetrics.IsAllowed(metricKey))
        {
            logger.LogDebug("Rejected unknown product metric {MetricKey}", metricKey);
            return;
        }

        db.AppMetrics.Add(new AppMetric
        {
            Id = Guid.NewGuid(),
            MetricKey = metricKey,
            MetricValue = value,
            DimensionsJson = dimensionsJson,
            RecordedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Totals for every allowlisted key since <paramref name="since"/>.</summary>
    public async Task<Dictionary<string, double>> SummarizeAsync(
        DateTimeOffset since,
        CancellationToken ct = default)
    {
        var rows = await db.AppMetrics
            .AsNoTracking()
            .Where(m => m.RecordedAt >= since)
            .GroupBy(m => m.MetricKey)
            .Select(g => new { Key = g.Key, Total = g.Sum(m => m.MetricValue) })
            .ToListAsync(ct);

        var summary = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var key in ProductMetrics.Allowed)
        {
            summary[key] = 0;
        }

        foreach (var row in rows)
        {
            summary[row.Key] = row.Total;
        }

        return summary;
    }

    /// <summary>True once any metric has ever been written, so admin can distinguish "zero" from "not wired".</summary>
    public Task<bool> HasAnyAsync(CancellationToken ct = default) =>
        db.AppMetrics.AsNoTracking().AnyAsync(ct);
}
