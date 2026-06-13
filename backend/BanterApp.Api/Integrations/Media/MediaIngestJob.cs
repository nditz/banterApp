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

    [AutomaticRetry(Attempts = 1, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var run = await _tracker.StartAsync(Provider, JobId, cancellationToken);
        var created = 0;
        var updated = 0;

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
                    var (c, u) = await UpsertItemAsync(source, video, cancellationToken);
                    created += c;
                    updated += u;
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
                    var (c, u) = await UpsertItemAsync(source, episode, cancellationToken);
                    created += c;
                    updated += u;
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
                    var (c, u) = await UpsertItemAsync(source, article, cancellationToken);
                    created += c;
                    updated += u;
                }
            }

            if (created > 0 || updated > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _tracker.CompleteAsync(run, created, updated, ct: cancellationToken);
            _logger.LogInformation("Media ingest: {Created} created, {Updated} updated.", created, updated);
        }
        catch (Exception ex)
        {
            await _tracker.CompleteAsync(run, created, updated, failed: 1, errorMessage: ex.Message, cancellationToken);
            throw;
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
        var existing = await _db.MediaSources.FirstOrDefaultAsync(
            x => x.SourceType == sourceType && x.ExternalId == externalId,
            ct);

        if (existing is not null)
        {
            return existing;
        }

        var source = new MediaSource
        {
            Id = Guid.NewGuid(),
            Name = name,
            SourceType = sourceType,
            ExternalId = externalId,
            RssUrl = rssUrl,
            SiteUrl = siteUrl,
            CrawlAllowed = sourceType != "website",
            ExtractPredictions = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MediaSources.Add(source);
        await _db.SaveChangesAsync(ct);
        return source;
    }

    private async Task<(int Created, int Updated)> UpsertItemAsync(
        MediaSource source,
        Dtos.MediaItemDto item,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MediaItems.FirstOrDefaultAsync(
            x => x.MediaSourceId == source.Id && x.ExternalId == item.ExternalId,
            cancellationToken);

        if (existing is null)
        {
            _db.MediaItems.Add(new MediaItem
            {
                Id = Guid.NewGuid(),
                MediaSourceId = source.Id,
                ExternalId = item.ExternalId,
                Title = item.Title,
                Description = item.Description,
                SourceUrl = item.SourceUrl,
                AudioUrl = item.AudioUrl,
                PublishedAt = item.PublishedAt,
                TranscriptSnippet = Truncate(item.Description, 280),
                LastSyncedAt = DateTimeOffset.UtcNow
            });
            return (1, 0);
        }

        var changed = existing.Title != item.Title ||
                      existing.Description != item.Description ||
                      existing.SourceUrl != item.SourceUrl;

        if (changed)
        {
            existing.Title = item.Title;
            existing.Description = item.Description;
            existing.SourceUrl = item.SourceUrl;
            existing.AudioUrl = item.AudioUrl;
            existing.PublishedAt = item.PublishedAt;
            existing.TranscriptSnippet = Truncate(item.Description, 280);
            existing.LastSyncedAt = DateTimeOffset.UtcNow;
            return (0, 1);
        }

        return (0, 0);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}
