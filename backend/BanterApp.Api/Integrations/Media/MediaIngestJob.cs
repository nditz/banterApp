using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Rss;
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
    private readonly IRssFeedCatalog _catalog;
    private readonly AppDbContext _db;
    private readonly MediaIngestOptions _options;
    private readonly SyncRunTracker _tracker;
    private readonly ILogger<MediaIngestJob> _logger;

    public MediaIngestJob(
        IYouTubeProvider youtube,
        IRssFeedProvider rss,
        IRssFeedCatalog catalog,
        AppDbContext db,
        IOptions<MediaIngestOptions> options,
        SyncRunTracker tracker,
        ILogger<MediaIngestJob> logger)
    {
        _youtube = youtube;
        _rss = rss;
        _catalog = catalog;
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
            foreach (var channel in ResolveYouTubeSources())
            {
                if (!channel.ExtractPredictions)
                {
                    continue;
                }

                var source = await EnsureSourceAsync(
                    channel.Name,
                    "youtube",
                    channel.ExternalId,
                    rssUrl: null,
                    siteUrl: channel.SiteUrl,
                    ct: cancellationToken);

                var videos = await _youtube.GetChannelVideosAsync(
                    channel.ExternalId,
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

            foreach (var podcast in await ResolvePodcastSourcesAsync(cancellationToken))
            {
                if (!podcast.ExtractPredictions)
                {
                    continue;
                }

                var source = await EnsureSourceAsync(
                    podcast.Name,
                    "podcast",
                    podcast.ExternalId,
                    rssUrl: podcast.RssUrl,
                    siteUrl: podcast.SiteUrl,
                    ct: cancellationToken);

                var episodes = await _rss.FetchFeedAsync(
                    podcast.RssUrl,
                    _options.MaxItemsPerSource,
                    cancellationToken);

                foreach (var episode in episodes)
                {
                    var (c, u, f) = await UpsertItemSafeAsync(source, episode, run.Id, cancellationToken);
                    created += c;
                    updated += u;
                    failed += f;
                }
            }

            foreach (var website in await ResolveWebsiteSourcesAsync(cancellationToken))
            {
                var source = await EnsureSourceAsync(
                    website.Name,
                    website.Type,
                    website.ExternalId,
                    rssUrl: website.RssUrl,
                    siteUrl: website.SiteUrl,
                    ct: cancellationToken);

                var articles = await _rss.FetchFeedAsync(
                    website.RssUrl,
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

    private IEnumerable<(string Name, string ExternalId, string? SiteUrl, string? RssUrl, bool ExtractPredictions)>
        ResolveYouTubeSources()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var channel in _options.YouTubeChannels.Where(c => !string.IsNullOrWhiteSpace(c.ChannelId)))
        {
            var id = channel.ChannelId.Trim();
            if (!seen.Add(id))
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(channel.Name) ? $"YouTube · {id}" : channel.Name.Trim();
            var siteUrl = channel.SiteUrl?.Trim()
                ?? $"https://www.youtube.com/channel/{id}";

            yield return (name, id, siteUrl, null, channel.ExtractPredictions);
        }

        foreach (var channelId in _options.YouTubeChannelIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            var id = channelId.Trim();
            if (!seen.Add(id))
            {
                continue;
            }

            yield return (
                $"YouTube · {id}",
                id,
                $"https://www.youtube.com/channel/{id}",
                null,
                true);
        }
    }

    private async Task<IReadOnlyList<(string Name, string ExternalId, string RssUrl, string? SiteUrl, bool ExtractPredictions)>>
        ResolvePodcastSourcesAsync(CancellationToken ct)
    {
        var catalog = (await _catalog.GetActiveForMediaIngestAsync(ct))
            .Where(f => f.Kind == RssFeedKind.Podcast)
            .ToList();

        if (catalog.Count > 0)
        {
            return catalog
                .Select(f => (
                    f.Name,
                    ExternalId: f.ApplePodcastId is > 0 ? $"apple:{f.ApplePodcastId}" : f.Slug,
                    f.RssUrl,
                    f.SiteUrl,
                    f.ExtractPredictions))
                .ToList();
        }

        return ResolvePodcastSourcesFromConfig().ToList();
    }

    private async Task<IReadOnlyList<(string Name, string Type, string ExternalId, string RssUrl, string? SiteUrl)>>
        ResolveWebsiteSourcesAsync(CancellationToken ct)
    {
        var catalog = (await _catalog.GetActiveForMediaIngestAsync(ct))
            .Where(f => f.Kind == RssFeedKind.Website)
            .ToList();

        if (catalog.Count > 0)
        {
            return catalog
                .Select(f => (f.Name, Type: "website", ExternalId: f.Slug, f.RssUrl, f.SiteUrl))
                .ToList();
        }

        return _options.WebsiteSources
            .Where(w => !string.IsNullOrWhiteSpace(w.RssUrl) && w.CrawlAllowed != false)
            .Select(w => (
                w.Name,
                Type: string.IsNullOrWhiteSpace(w.Type) ? "website" : w.Type,
                ExternalId: w.RssUrl!,
                RssUrl: w.RssUrl!,
                SiteUrl: w.BaseUrl))
            .ToList();
    }

    private IEnumerable<(string Name, string ExternalId, string RssUrl, string? SiteUrl, bool ExtractPredictions)>
        ResolvePodcastSourcesFromConfig()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var podcast in _options.PodcastSources.Where(p => !string.IsNullOrWhiteSpace(p.RssUrl)))
        {
            var rssUrl = podcast.RssUrl.Trim();
            if (!seen.Add(rssUrl))
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(podcast.Name) ? rssUrl : podcast.Name.Trim();
            yield return (name, rssUrl, rssUrl, podcast.SiteUrl?.Trim(), podcast.ExtractPredictions);
        }

        foreach (var feedUrl in _options.PodcastFeedUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            var rssUrl = feedUrl.Trim();
            if (!seen.Add(rssUrl))
            {
                continue;
            }

            yield return (rssUrl, rssUrl, rssUrl, null, true);
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

        if (existing is null && !string.IsNullOrWhiteSpace(rssUrl))
        {
            existing = await _db.MediaSources.FirstOrDefaultAsync(
                x => x.SourceType == sourceType && x.RssUrl == rssUrl,
                ct);
        }

        if (existing is not null)
        {
            var displayName = StringLimits.Truncate(name, 120) ?? name;
            if (!string.Equals(existing.Name, displayName, StringComparison.Ordinal))
            {
                existing.Name = displayName;
            }

            existing.SiteUrl = StringLimits.Truncate(siteUrl, 512) ?? existing.SiteUrl;
            existing.RssUrl = StringLimits.Truncate(rssUrl, 512) ?? existing.RssUrl;
            if (!string.Equals(existing.ExternalId, normalizedExternalId, StringComparison.Ordinal))
            {
                existing.ExternalId = normalizedExternalId;
            }
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
                PublishedAt = PostgresUtc.Normalize(item.PublishedAt),
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
            existing.PublishedAt = PostgresUtc.Normalize(item.PublishedAt);
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
