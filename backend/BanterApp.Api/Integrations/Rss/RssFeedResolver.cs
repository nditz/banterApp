using System.Net;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Integrations.Rss;

public sealed record RssFeedResolveResult(int Checked, int Updated, int Deactivated, int Failed);

public sealed class RssFeedResolver(
    AppDbContext db,
    ISafeHttpClient http,
    IRssUrlDiscovery urlDiscovery,
    ILogger<RssFeedResolver> logger)
{
    public const int ConsecutiveFailuresToDisable = 3;
    public const int HealthySkipMinutes = 20;

    public Task<RssFeedResolveResult> EnsureUrlsAsync(CancellationToken ct = default) =>
        ResolveAsync(ct, TimeSpan.FromMinutes(HealthySkipMinutes), allowAiDiscovery: false);

    public async Task<RssFeedResolveResult> ResolveAsync(
        CancellationToken ct = default,
        TimeSpan? skipHealthyWithin = null,
        bool allowAiDiscovery = true)
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
            if (ShouldSkipHealthy(feed, skipHealthyWithin))
            {
                continue;
            }

            if (RssSourcePolicy.IsDisallowed(feed.RssUrl))
            {
                if (feed.IsActive)
                {
                    Deactivate(feed, "Disallowed source host");
                    deactivated++;
                }

                continue;
            }

            var wasActive = feed.IsActive;
            try
            {
                var changed = await ResolveOneAsync(feed, allowAiDiscovery, ct);
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
                logger.LogWarning(
                    ex,
                    "RSS feed resolve failed for {Slug} ({Url}) job={JobId}.",
                    feed.Slug,
                    feed.RssUrl,
                    RssFeedResolveJob.JobId);
            }
        }

        await db.SaveChangesAsync(ct);
        return new RssFeedResolveResult(feeds.Count, updated, deactivated, failed);
    }

    private async Task<bool> ResolveOneAsync(RssFeed feed, bool allowAiDiscovery, CancellationToken ct)
    {
        var urlChanged = false;
        var previousUrl = feed.RssUrl;

        if (feed.ApplePodcastId is > 0)
        {
            try
            {
                var lookup = await http.FetchAsync(ApplePodcastLookup.LookupUrl(feed.ApplePodcastId.Value), ct);
                if (lookup.FailureKind is not SafeHttpFailureKind.None)
                {
                    logger.LogWarning(
                        "Apple Podcasts lookup failed for {Slug} id={AppleId}: {Kind} {Reason}.",
                        feed.Slug,
                        feed.ApplePodcastId,
                        lookup.FailureKind,
                        lookup.FailureReason);
                }

                var appleUrl = ApplePodcastLookup.ParseFeedUrl(lookup.Response?.Content);
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
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Apple Podcasts lookup threw for {Slug} id={AppleId}.",
                    feed.Slug,
                    feed.ApplePodcastId);
            }
        }

        if (!RssUrlNormalizer.IsAbsoluteHttpUrl(feed.RssUrl))
        {
            if (allowAiDiscovery &&
                await TryApplyDiscoveredUrlAsync(feed, previousUrl, "missing_or_invalid_url", ct))
            {
                return true;
            }

            feed.LastCheckedAt = DateTimeOffset.UtcNow;
            return urlChanged;
        }

        var fetch = await http.FetchAsync(feed.RssUrl, RssFeedProvider.MaxResponseBytes, ct);
        feed.LastCheckedAt = DateTimeOffset.UtcNow;
        feed.UpdatedAt = DateTimeOffset.UtcNow;

        if (IsHealthyProbe(fetch))
        {
            return await CompleteHealthyProbeAsync(feed, fetch, previousUrl, urlChanged, ct);
        }

        var failureReason = DescribeProbeFailure(fetch);
        logger.LogWarning(
            "RSS feed probe failed for {Slug} ({Url}): {Reason}.",
            feed.Slug,
            feed.RssUrl,
            failureReason);

        if (allowAiDiscovery &&
            await TryApplyDiscoveredUrlAsync(feed, previousUrl, failureReason, ct))
        {
            return true;
        }

        RecordProbeFailure(feed, fetch);
        return urlChanged;
    }

    private async Task<bool> TryApplyDiscoveredUrlAsync(
        RssFeed feed,
        string previousUrl,
        string failureReason,
        CancellationToken ct)
    {
        string? suggested;
        try
        {
            suggested = await urlDiscovery.SuggestFeedUrlAsync(feed, failureReason, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RSS URL discovery threw for {Slug}.", feed.Slug);
            return false;
        }

        if (!RssUrlNormalizer.IsAbsoluteHttpUrl(suggested) ||
            RssUrlNormalizer.EqualsUrl(suggested, feed.RssUrl) ||
            RssSourcePolicy.IsDisallowed(suggested))
        {
            return false;
        }

        var fetch = await http.FetchAsync(suggested!, RssFeedProvider.MaxResponseBytes, ct);
        if (!IsHealthyProbe(fetch))
        {
            logger.LogWarning(
                "OpenAI-suggested RSS URL failed probe for {Slug}: {Url} ({Reason}).",
                feed.Slug,
                suggested,
                DescribeProbeFailure(fetch));
            return false;
        }

        logger.LogInformation(
            "RSS feed {Slug} OpenAI discovery updated URL {Old} -> {New}.",
            feed.Slug,
            feed.RssUrl,
            suggested);
        feed.RssUrl = StringLimits.Truncate(suggested, 512) ?? suggested!;
        await CompleteHealthyProbeAsync(feed, fetch, previousUrl, urlChanged: true, ct);
        return true;
    }

    private async Task<bool> CompleteHealthyProbeAsync(
        RssFeed feed,
        SafeHttpFetchResult fetch,
        string previousUrl,
        bool urlChanged,
        CancellationToken ct)
    {
        var response = fetch.Response!;
        feed.LastCheckedAt = DateTimeOffset.UtcNow;
        feed.UpdatedAt = DateTimeOffset.UtcNow;
        feed.LastHttpStatus = (int)response.StatusCode;
        feed.ConsecutiveFailures = 0;
        if (!feed.IsActive)
        {
            feed.IsActive = true;
            logger.LogInformation("RSS feed {Slug} reactivated after a healthy probe.", feed.Slug);
        }

        if (fetch.FailureKind == SafeHttpFailureKind.Oversized)
        {
            logger.LogWarning(
                "RSS feed probe truncated for {Slug} ({Url}): {Reason}.",
                feed.Slug,
                feed.RssUrl,
                fetch.FailureReason);
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

    private void RecordProbeFailure(RssFeed feed, SafeHttpFetchResult fetch)
    {
        var response = fetch.Response;
        if (response is not null)
        {
            feed.LastHttpStatus = (int)response.StatusCode;
        }

        if (response?.StatusCode == HttpStatusCode.Gone)
        {
            Deactivate(feed, "HTTP 410 Gone");
            return;
        }

        if (fetch.FailureKind == SafeHttpFailureKind.Oversized)
        {
            return;
        }

        if (fetch.FailureKind == SafeHttpFailureKind.None &&
            response is not null &&
            IsSuccess(response.StatusCode) &&
            !RssUrlNormalizer.LooksLikeFeed(response.Content))
        {
            return;
        }

        feed.ConsecutiveFailures++;
        if (feed.ConsecutiveFailures >= ConsecutiveFailuresToDisable)
        {
            Deactivate(feed, $"{DescribeProbeFailure(fetch)} x{feed.ConsecutiveFailures}");
        }
    }

    private static bool IsHealthyProbe(SafeHttpFetchResult fetch)
    {
        var response = fetch.Response;
        if (response is null || string.IsNullOrWhiteSpace(response.Content))
        {
            return false;
        }

        if (fetch.FailureKind is not SafeHttpFailureKind.None and not SafeHttpFailureKind.Oversized
            and not SafeHttpFailureKind.HttpStatus)
        {
            return false;
        }

        if (fetch.FailureKind == SafeHttpFailureKind.HttpStatus && !IsSuccess(response.StatusCode))
        {
            return false;
        }

        if (!IsSuccess(response.StatusCode) && fetch.FailureKind != SafeHttpFailureKind.Oversized)
        {
            return false;
        }

        return RssUrlNormalizer.LooksLikeFeed(response.Content);
    }

    private static string DescribeProbeFailure(SafeHttpFetchResult fetch)
    {
        if (fetch.Response?.StatusCode == HttpStatusCode.Gone)
        {
            return "http_410";
        }

        if (fetch.FailureKind == SafeHttpFailureKind.None &&
            fetch.Response is { } response &&
            IsSuccess(response.StatusCode) &&
            !RssUrlNormalizer.LooksLikeFeed(response.Content))
        {
            return "not_rss_or_atom";
        }

        return fetch.FailureReason ?? fetch.FailureKind.ToString();
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

    private static bool ShouldSkipHealthy(RssFeed feed, TimeSpan? skipHealthyWithin)
    {
        if (skipHealthyWithin is not { } window || window <= TimeSpan.Zero)
        {
            return false;
        }

        if (!feed.IsActive || feed.ConsecutiveFailures > 0)
        {
            return false;
        }

        if (feed.LastCheckedAt is not { } checkedAt)
        {
            return false;
        }

        if (!RssUrlNormalizer.IsAbsoluteHttpUrl(feed.RssUrl))
        {
            return false;
        }

        return DateTimeOffset.UtcNow - checkedAt < window;
    }

    private static bool IsSuccess(HttpStatusCode status) =>
        (int)status is >= 200 and <= 299;
}
