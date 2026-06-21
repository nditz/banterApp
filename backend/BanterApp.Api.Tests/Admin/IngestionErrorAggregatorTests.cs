using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class IngestionErrorAggregatorTests
{
    [Fact]
    public async Task SyncFromLogsAsync_GroupsDuplicateApplicationErrors()
    {
        await using var db = TestDbContextFactory.Create();
        db.ApplicationErrorLogs.Add(new ApplicationErrorLog
        {
            Id = Guid.NewGuid(),
            Source = "background",
            Category = "pundit-extraction",
            Message = "OpenAI timeout",
            OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var aggregator = new IngestionErrorAggregator(db);
        await aggregator.SyncFromLogsAsync();
        await aggregator.SyncFromLogsAsync();

        var errors = await db.IngestionErrors.ToListAsync();
        var grouped = errors.Single(e => e.Message == "OpenAI timeout");

        Assert.Equal("open", grouped.Status);
        Assert.Equal(2, grouped.Count);
    }

    [Fact]
    public async Task SyncFromLogsAsync_CreatesErrorFromFailedMediaItem()
    {
        await using var db = TestDbContextFactory.Create();
        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = "Test RSS",
            SourceType = "rss",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MediaSources.Add(source);
        db.MediaItems.Add(new MediaItem
        {
            Id = Guid.NewGuid(),
            MediaSourceId = source.Id,
            ExternalId = "item-1",
            Title = "Failed item",
            SourceUrl = "https://example.com/1",
            ProcessingStatus = MediaItemProcessingStatus.Failed,
            ProcessingError = "Transcript unavailable",
            ProcessedAt = DateTimeOffset.UtcNow,
            LastSyncedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var aggregator = new IngestionErrorAggregator(db);
        await aggregator.SyncFromLogsAsync();

        Assert.Contains(
            await db.IngestionErrors.Select(e => e.Message).ToListAsync(),
            m => m.Contains("Transcript unavailable", StringComparison.Ordinal));
    }
}
