using System.Text.Json;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media.Dtos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BanterApp.Api.Integrations.Pundits;

public sealed class PunditMediaItemService
{
    private readonly AppDbContext _db;

    public PunditMediaItemService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MediaSource> EnsureSourceAsync(
        string name,
        string sourceType,
        string externalId,
        string? rssUrl = null,
        string? siteUrl = null,
        string? configJson = null,
        CancellationToken ct = default)
    {
        var normalizedExternalId = ExternalIdNormalizer.Normalize(externalId);
        var existing = await FindSourceAsync(sourceType, normalizedExternalId, rssUrl, ct);

        if (existing is not null)
        {
            existing.Name = StringLimits.Truncate(name, 120) ?? name;
            existing.SiteUrl = StringLimits.Truncate(siteUrl, 512) ?? existing.SiteUrl;
            existing.RssUrl = StringLimits.Truncate(rssUrl, 512) ?? existing.RssUrl;
            existing.ConfigJson = configJson ?? existing.ConfigJson;
            existing.ExtractPredictions = true;
            existing.IsActive = true;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            return existing;
        }

        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = StringLimits.Truncate(name, 120) ?? name,
            SourceType = sourceType,
            ExternalId = normalizedExternalId,
            RssUrl = StringLimits.Truncate(rssUrl, 512),
            SiteUrl = StringLimits.Truncate(siteUrl, 512),
            ConfigJson = configJson,
            CrawlAllowed = true,
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.MediaSources.Add(source);
        // Persist on the next item/feed SaveChanges. Saving here flushes every pending
        // media_items row and is what production logged at EnsureSourceAsync line 62.
        return source;
    }

    public async Task<(int Created, int Updated, int Skipped, bool TextChanged)> UpsertItemAsync(
        MediaSource source,
        MediaItemDto item,
        CancellationToken cancellationToken)
    {
        var externalId = ExternalIdNormalizer.Normalize(item.ExternalId);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return (0, 0, 1, false);
        }

        var hash = ContentHashHelper.Compute(externalId, item.SourceUrl, item.Title);

        if (!source.IsActive)
        {
            return (0, 0, 1, false);
        }

        var duplicateHash = _db.MediaItems.Local.Any(x =>
                x.ContentHash == hash && x.MediaSourceId != source.Id)
            || await _db.MediaItems
                .AnyAsync(x => x.ContentHash == hash && x.MediaSourceId != source.Id, cancellationToken);
        if (duplicateHash)
        {
            return (0, 0, 1, false);
        }

        var existing = FindTrackedItem(source.Id, externalId)
            ?? await _db.MediaItems.FirstOrDefaultAsync(
                x => x.MediaSourceId == source.Id && x.ExternalId == externalId,
                cancellationToken);

        var rawSummary = item.Description;
        var rawText = item.FullText ?? item.Description;
        var payload = JsonSerializer.Serialize(new
        {
            item.Author,
            item.Publication,
            item.SourceExternalId
        });

        if (existing is null)
        {
            _db.MediaItems.Add(new MediaItem
            {
                Id = Guid.NewGuid(),
                MediaSourceId = source.Id,
                ExternalId = externalId,
                Title = StringLimits.Truncate(item.Title, 300) ?? string.Empty,
                Description = item.Description,
                SourceUrl = StringLimits.Truncate(item.SourceUrl, 512) ?? string.Empty,
                AudioUrl = StringLimits.Truncate(item.AudioUrl, 512),
                PublishedAt = PostgresUtc.Normalize(item.PublishedAt),
                Author = StringLimits.Truncate(item.Author, StringLimits.MediaAuthor),
                Publication = StringLimits.Truncate(item.Publication ?? source.Name, StringLimits.MediaPublication),
                RawSummary = rawSummary,
                RawText = rawText,
                RawPayloadJson = payload,
                ContentHash = StringLimits.Truncate(hash, StringLimits.ContentHash),
                TranscriptSnippet = StringLimits.Truncate(rawText, 280),
                ProcessingStatus = MediaItemProcessingStatus.Pending,
                LastSyncedAt = DateTimeOffset.UtcNow
            });
            return await PersistUpsertAsync((1, 0, 0, true), cancellationToken);
        }

        var textChanged = existing.RawText != rawText;
        var metadataChanged =
            existing.Title != item.Title ||
            existing.Description != item.Description ||
            existing.SourceUrl != item.SourceUrl ||
            existing.Author != item.Author;

        if (metadataChanged || textChanged)
        {
            existing.Title = StringLimits.Truncate(item.Title, 300) ?? string.Empty;
            existing.Description = item.Description;
            existing.SourceUrl = StringLimits.Truncate(item.SourceUrl, 512) ?? string.Empty;
            existing.AudioUrl = StringLimits.Truncate(item.AudioUrl, 512);
            existing.PublishedAt = PostgresUtc.Normalize(item.PublishedAt);
            existing.Author = StringLimits.Truncate(item.Author, StringLimits.MediaAuthor);
            existing.Publication = StringLimits.Truncate(item.Publication ?? source.Name, StringLimits.MediaPublication);
            existing.RawSummary = rawSummary;
            if (textChanged)
            {
                existing.RawText = rawText;
                existing.ProcessingStatus = MediaItemProcessingStatus.Pending;
                existing.ProcessedAt = null;
            }

            existing.RawPayloadJson = payload;
            existing.ContentHash = StringLimits.Truncate(hash, StringLimits.ContentHash);
            existing.TranscriptSnippet = StringLimits.Truncate(rawText, 280);
            existing.LastSyncedAt = DateTimeOffset.UtcNow;
            return await PersistUpsertAsync((0, 1, 0, textChanged), cancellationToken);
        }

        return (0, 0, 1, false);
    }

    private async Task<(int Created, int Updated, int Skipped, bool TextChanged)> PersistUpsertAsync(
        (int Created, int Updated, int Skipped, bool TextChanged) result,
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            DiscardAddedMediaItems();
            return (0, 0, 1, false);
        }
    }

    private async Task<MediaSource?> FindSourceAsync(
        string sourceType,
        string normalizedExternalId,
        string? rssUrl,
        CancellationToken ct)
    {
        var local = _db.MediaSources.Local.FirstOrDefault(x =>
            x.SourceType == sourceType && x.ExternalId == normalizedExternalId);
        if (local is not null)
        {
            return local;
        }

        var existing = await _db.MediaSources.FirstOrDefaultAsync(
            x => x.SourceType == sourceType && x.ExternalId == normalizedExternalId,
            ct);
        if (existing is not null)
        {
            return existing;
        }

        if (string.IsNullOrWhiteSpace(rssUrl))
        {
            return null;
        }

        local = _db.MediaSources.Local.FirstOrDefault(x =>
            x.SourceType == sourceType && x.RssUrl == rssUrl);
        if (local is not null)
        {
            return local;
        }

        return await _db.MediaSources.FirstOrDefaultAsync(
            x => x.SourceType == sourceType && x.RssUrl == rssUrl,
            ct);
    }

    private MediaItem? FindTrackedItem(Guid sourceId, string externalId) =>
        _db.MediaItems.Local.FirstOrDefault(x =>
            x.MediaSourceId == sourceId && x.ExternalId == externalId);

    private void DiscardAddedMediaItems()
    {
        foreach (var entry in _db.ChangeTracker.Entries<MediaItem>()
                     .Where(e => e.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg &&
        pg.SqlState == PostgresErrorCodes.UniqueViolation;
}
