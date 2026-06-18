using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Discovers YouTube videos, podcast episodes, and website RSS items for pundit prediction extraction.
/// </summary>
public sealed class MediaIngestJob
{
    public const string JobId = "media-ingest";
    private const string Provider = "media";

    private readonly IYouTubeProvider _youtube;
    private readonly IRssFeedProvider _rss;
    private readonly AppDbContext _db;
    private readonly MediaIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<MediaIngestJob> _logger;

    public MediaIngestJob(
        IYouTubeProvider youtube,
        IRssFeedProvider rss,
        AppDbContext db,
        IOptions<MediaIngestOptions> options,
        SyncRunTracker tracker,
        ILogger<MediaIngestJob> logger)
    {
        _youtube = youtube;
        _rss = rss;
        _db = db;
        _options = options.Value;
        _tracker = tracker;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;
        var failed = 0;

        try
        {
            foreach (var channelId in _options.YouTubeChannelIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            {
                var source = await EnsureSourceAsync(
                    $"YouTube:{channelId}",
                    "youtube",
                    channelId,
                    rssUrl: null,
                    siteUrl: $"https://www.youtube.com/channel/{channelId}",
                    ct: cancellationToken);

                var videos = await _youtube.GetChannelVideosAsync(
                    channelId,
                    _options.MaxItemsPerSource,
                    cancellationToken);

                foreach (var video in videos)
                {
                    var (c, u, f) = await UpsertItemSafeAsync(source, video, run.Id, cancellationToken);
                    created += c;
                    updated += u;
                    failed += f;
                }
            }

            foreach (var feedUrl in _options.PodcastFeedUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                var source = await EnsureSourceAsync(
                    feedUrl,
                    "podcast",
                    feedUrl,
                    rssUrl: feedUrl,
                    siteUrl: null,
                    ct: cancellationToken);

                var episodes = await _rss.FetchFeedAsync(feedUrl, _options.MaxItemsPerSource, cancellationToken);
                foreach (var episode in episodes)
                {
                    var (c, u, f) = await UpsertItemSafeAsync(source, episode, run.Id, cancellationToken);
                    created += c;
                    updated += u;
                    failed += f;
                }
            }

            foreach (var website in _options.WebsiteSources.Where(w => !string.IsNullOrWhiteSpace(w.RssUrl)))
            {
                if (website.CrawlAllowed == false)
                {
                    continue;
                }

                var source = await EnsureSourceAsync(
                    website.Name,
                    website.Type,
                    website.RssUrl ?? website.Name,
                    rssUrl: website.RssUrl,
                    siteUrl: website.BaseUrl,
                    ct: cancellationToken);

                var articles = await _rss.FetchFeedAsync(
                    website.RssUrl!,
                    _options.MaxItemsPerSource,
                    cancellationToken);

                foreach (var article in articles)
                {
                    var (c, u, f) = await UpsertItemSafeAsync(source, article, run.Id, cancellationToken);
                    created += c;
                    updated += u;
                    failed += f;
                }
            }

            if (created > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _tracker.CompleteAsync(run, created, updated, failed, ct: cancellationToken);
            _logger.LogInformation(
                "Media ingest: {Created} created, {Updated} updated, {Failed} failed.",
                created,
                updated,
                failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Media ingest job failed.");
            await _tracker.FailAsync(run, created, updated, ex, cancellationToken);
        }
    }

    private async Task<MediaSource> EnsureSourceAsync(
        string name,
        string sourceType,
        string externalId,
        string? rssUrl = null,
        string? siteUrl = null,
        CancellationToken ct = default)
    {
        var normalizedExternalId = ExternalIdNormalizer.Normalize(externalId);
        var existing = await _db.MediaSources.FirstOrDefaultAsync(
            x => x.SourceType == sourceType && x.ExternalId == normalizedExternalId,
            ct);

        if (existing is not null)
        {
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
            CrawlAllowed = sourceType != "website",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MediaSources.Add(source);
        await _db.SaveChangesAsync(ct);
        return source;
    }

    private async Task<(int Created, int Updated, int Failed)> UpsertItemSafeAsync(
        MediaSource source,
        Dtos.MediaItemDto item,
        Guid syncRunId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await UpsertItemAsync(source, item, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to upsert media item {ExternalId}.", item.ExternalId);
            await _tracker.LogErrorAsync(
                Provider,
                JobId,
                "media_item",
                ex.Message,
                syncRunId,
                ExternalIdNormalizer.Normalize(item.ExternalId),
                cancellationToken);
            return (0, 0, 1);
        }
    }

    private async Task<(int Created, int Updated, int Failed)> UpsertItemAsync(
        MediaSource source,
        Dtos.MediaItemDto item,
        CancellationToken cancellationToken)
    {
        var externalId = ExternalIdNormalizer.Normalize(item.ExternalId);
        var existing = await _db.MediaItems.FirstOrDefaultAsync(
            x => x.MediaSourceId == source.Id && x.ExternalId == externalId,
            cancellationToken);

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
                PublishedAt = item.PublishedAt,
                TranscriptSnippet = Truncate(item.Description, 280),
                LastSyncedAt = DateTimeOffset.UtcNow
            });
            return (1, 0, 0);
        }

        var changed = existing.Title != item.Title ||
                      existing.Description != item.Description ||
                      existing.SourceUrl != item.SourceUrl;

        if (changed)
        {
            existing.Title = StringLimits.Truncate(item.Title, 300) ?? string.Empty;
            existing.Description = item.Description;
            existing.SourceUrl = StringLimits.Truncate(item.SourceUrl, 512) ?? string.Empty;
            existing.AudioUrl = StringLimits.Truncate(item.AudioUrl, 512);
            existing.PublishedAt = item.PublishedAt;
            existing.TranscriptSnippet = Truncate(item.Description, 280);
            existing.LastSyncedAt = DateTimeOffset.UtcNow;
            return (0, 1, 0);
        }

        return (0, 0, 0);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return StringLimits.Truncate(value, maxLength);
    }
}
