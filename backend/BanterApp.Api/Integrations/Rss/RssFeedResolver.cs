using System.Net;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Rss;

public sealed record RssFeedResolveResult(int Checked, int Updated, int Deactivated, int Failed);

public sealed class RssFeedResolver(
    AppDbContext db,
    ISafeHttpClient http,
    ILogger<RssFeedResolver> logger)
{
    public const int ConsecutiveFailuresToDisable = 3;

    public async Task<RssFeedResolveResult> ResolveAsync(CancellationToken ct = default)
    {
        var feeds = await db.RssFeeds
            .Where(f => f.IsActive || f.ApplePodcastId != null)
            .OrderByDescending(f => f.Priority)
            .ThenBy(f => f.Name)
            .ToListAsync(ct);

        var updated = 0;
        var deactivated = 0;
        var failed = 0;

        foreach (var feed in feeds)
        {
            var wasActive = feed.IsActive;
            try
            {
                var changed = await ResolveOneAsync(feed, ct);
                if (changed)
                {
                    updated++;
                }

                if (wasActive && !feed.IsActive)
                {
                    deactivated++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "RSS feed resolve failed for {Slug} ({Url}).", feed.Slug, feed.RssUrl);
            }
        }

        await db.SaveChangesAsync(ct);
        return new RssFeedResolveResult(feeds.Count, updated, deactivated, failed);
    }

    private async Task<bool> ResolveOneAsync(RssFeed feed, CancellationToken ct)
    {
        var urlChanged = false;
        var previousUrl = feed.RssUrl;

        if (feed.ApplePodcastId is > 0)
        {
            var lookup = await http.GetStringAsync(ApplePodcastLookup.LookupUrl(feed.ApplePodcastId.Value), ct);
            var appleUrl = ApplePodcastLookup.ParseFeedUrl(lookup?.Content);
            if (RssUrlNormalizer.IsAbsoluteHttpUrl(appleUrl) &&
                !RssUrlNormalizer.EqualsUrl(feed.RssUrl, appleUrl))
            {
                logger.LogInformation(
                    "RSS feed {Slug} Apple lookup updated URL {Old} -> {New}.",
                    feed.Slug,
                    feed.RssUrl,
                    appleUrl);
                feed.RssUrl = StringLimits.Truncate(appleUrl, 512) ?? appleUrl!;
                urlChanged = true;
            }
        }

        if (!RssUrlNormalizer.IsAbsoluteHttpUrl(feed.RssUrl))
        {
            feed.LastCheckedAt = DateTimeOffset.UtcNow;
            return urlChanged;
        }

        var response = await http.GetStringAsync(feed.RssUrl, ct);
        feed.LastCheckedAt = DateTimeOffset.UtcNow;
        feed.UpdatedAt = DateTimeOffset.UtcNow;

        if (response is null)
        {
            return urlChanged;
        }

        feed.LastHttpStatus = (int)response.StatusCode;

        if (response.StatusCode == HttpStatusCode.Gone)
        {
            Deactivate(feed, "HTTP 410 Gone");
            return urlChanged;
        }

        if (!IsSuccess(response.StatusCode))
        {
            feed.ConsecutiveFailures++;
            if (feed.ConsecutiveFailures >= ConsecutiveFailuresToDisable)
            {
                Deactivate(feed, $"HTTP {(int)response.StatusCode} x{feed.ConsecutiveFailures}");
            }

            return urlChanged;
        }

        if (!RssUrlNormalizer.LooksLikeFeed(response.Content))
        {
            logger.LogWarning(
                "RSS feed {Slug} returned HTTP {Status} that is not RSS/Atom.",
                feed.Slug,
                (int)response.StatusCode);
            return urlChanged;
        }

        feed.ConsecutiveFailures = 0;
        if (!feed.IsActive && feed.ApplePodcastId is > 0)
        {
            feed.IsActive = true;
            logger.LogInformation("RSS feed {Slug} reactivated after a healthy Apple/probe check.", feed.Slug);
        }

        if (!string.IsNullOrWhiteSpace(response.FinalUrl) &&
            RssUrlNormalizer.IsAbsoluteHttpUrl(response.FinalUrl) &&
            !RssUrlNormalizer.EqualsUrl(feed.RssUrl, response.FinalUrl))
        {
            logger.LogInformation(
                "RSS feed {Slug} followed redirect {Old} -> {New}.",
                feed.Slug,
                feed.RssUrl,
                response.FinalUrl);
            feed.RssUrl = StringLimits.Truncate(response.FinalUrl, 512) ?? response.FinalUrl;
            urlChanged = true;
        }

        if (urlChanged && !RssUrlNormalizer.EqualsUrl(previousUrl, feed.RssUrl))
        {
            await SyncMediaSourceUrlsAsync(previousUrl, feed.RssUrl, ct);
        }

        return urlChanged;
    }

    private async Task SyncMediaSourceUrlsAsync(string previousUrl, string newUrl, CancellationToken ct)
    {
        var sources = await db.MediaSources
            .Where(s => s.RssUrl == previousUrl)
            .ToListAsync(ct);

        foreach (var source in sources)
        {
            source.RssUrl = StringLimits.Truncate(newUrl, 512);
            source.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private void Deactivate(RssFeed feed, string reason)
    {
        if (feed.IsActive)
        {
            logger.LogWarning("Deactivating RSS feed {Slug}: {Reason}.", feed.Slug, reason);
        }

        feed.IsActive = false;
        feed.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static bool IsSuccess(HttpStatusCode status) =>
        (int)status is >= 200 and <= 299;
}
