using System.Xml;
using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media.Dtos;
using BanterApp.Api.Integrations.Rss;
using BanterApp.Api.Services;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Integrations.Media;

public interface IRssFeedProvider
{
    Task<RssFeedFetchResult> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default);

    Task<RssFeedFetchResult> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        string? publicationName,
        bool includeFullContent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RSS/Atom feed parser for podcasts and sports websites. Does not crawl HTML pages.
/// </summary>
public sealed class RssFeedProvider : IRssFeedProvider
{
    public const int MaxResponseBytes = 20 * 1024 * 1024;

    private readonly ISafeHttpClient _safeHttpClient;
    private readonly ILogger<RssFeedProvider> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public RssFeedProvider(
        ISafeHttpClient safeHttpClient,
        ILogger<RssFeedProvider> logger,
        IServiceScopeFactory scopeFactory)
    {
        _safeHttpClient = safeHttpClient;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public Task<RssFeedFetchResult> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default) =>
        FetchFeedAsync(feedUrl, maxItems, publicationName: null, includeFullContent: false, cancellationToken);

    public async Task<RssFeedFetchResult> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        string? publicationName,
        bool includeFullContent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return RssFeedFetchResult.Empty(feedUrl ?? string.Empty);
        }

        if (RssSourcePolicy.IsDisallowed(feedUrl))
        {
            _logger.LogInformation("Skipping disallowed RSS host {Url}.", feedUrl);
            return RssFeedFetchResult.Empty(feedUrl);
        }

        try
        {
            var fetch = await _safeHttpClient.FetchAsync(feedUrl, MaxResponseBytes, cancellationToken);
            var response = fetch.Response;
            if (fetch.FailureKind == SafeHttpFailureKind.Oversized &&
                !string.IsNullOrWhiteSpace(response?.Content))
            {
                var salvaged = TryParsePartialFeed(
                    response.Content,
                    feedUrl,
                    maxItems,
                    publicationName,
                    includeFullContent);
                if (salvaged.Count > 0)
                {
                    _logger.LogWarning(
                        "RSS feed {Url} exceeded size limit ({Reason}); parsed {Count} complete items from the truncated body.",
                        feedUrl,
                        fetch.FailureReason,
                        salvaged.Count);
                    return RssFeedFetchResult.Ok(salvaged, feedUrl);
                }
            }

            if (response is null || string.IsNullOrWhiteSpace(response.Content) ||
                fetch.FailureKind is not SafeHttpFailureKind.None)
            {
                var ssrfBlocked = fetch.FailureKind == SafeHttpFailureKind.Ssrf;
                var reason = MapFetchFailureReason(fetch.FailureKind);
                _logger.LogWarning(
                    "RSS fetch failed for {Url}: {Reason} ({Kind}) detail={Detail} jobKey={JobKey}.",
                    feedUrl,
                    fetch.FailureReason ?? reason,
                    fetch.FailureKind,
                    fetch.FailureReason,
                    HangfireJobAmbientContext.Current?.JobKey);
                var mappedReason = ssrfBlocked && !string.IsNullOrWhiteSpace(fetch.FailureReason)
                    ? fetch.FailureReason
                    : reason;
                var failure = await TrackRssErrorAsync(
                    mappedReason,
                    feedUrl,
                    (int?)response?.StatusCode,
                    ssrfBlocked,
                    fetch.FailureReason,
                    cancellationToken);
                return RssFeedFetchResult.Failed(feedUrl, failure);
            }

            return RssFeedFetchResult.Ok(
                ParseFeed(response.Content, feedUrl, maxItems, publicationName, includeFullContent),
                feedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "RSS fetch failed for {Url} jobKey={JobKey}.",
                feedUrl,
                HangfireJobAmbientContext.Current?.JobKey);
            await TrackRssExceptionAsync(ex, feedUrl, cancellationToken);
            return RssFeedFetchResult.Failed(
                feedUrl,
                new RssFeedFetchFailure(
                    "unavailable",
                    "We could not load this feed right now.",
                    ex.Message,
                    SsrfBlocked: false,
                    StatusCode: null));
        }
    }

    private async Task<RssFeedFetchFailure> TrackRssErrorAsync(
        string reason,
        string feedUrl,
        int? statusCode,
        bool ssrfBlocked,
        string? detail,
        CancellationToken ct)
    {
        var mapped = ProviderErrorMapper.MapRss(reason, statusCode, feedUrl, ssrfBlocked: ssrfBlocked);
        var ambient = HangfireJobAmbientContext.Current;
        var metadata = new Dictionary<string, object?>(mapped.Metadata ?? new Dictionary<string, object?>())
        {
            ["failure_detail"] = detail,
            ["hangfire_job_id"] = ambient?.HangfireJobId,
            ["job_type"] = ambient?.JobTypeName
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = mapped.Code,
            MessageSafe = mapped.SafeMessage,
            MessageInternal = detail ?? mapped.SafeMessage,
            Severity = "warning",
            Provider = "rss",
            JobKey = ambient?.JobKey,
            Route = feedUrl,
            IsRetryable = mapped.IsRetryable,
            Metadata = metadata
        }, ct);

        return new RssFeedFetchFailure(
            reason,
            mapped.SafeMessage,
            detail,
            ssrfBlocked,
            statusCode);
    }

    private async Task TrackRssExceptionAsync(Exception ex, string feedUrl, CancellationToken ct)
    {
        var ambient = HangfireJobAmbientContext.Current;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = ErrorCodes.RssFetchError,
            MessageSafe = ProviderErrorMapper.MapRss("unavailable", feedUrl: feedUrl).SafeMessage,
            Severity = "error",
            Provider = "rss",
            JobKey = ambient?.JobKey,
            Route = feedUrl,
            IsRetryable = true,
            Metadata = new Dictionary<string, object?>
            {
                ["feed_url"] = feedUrl,
                ["hangfire_job_id"] = ambient?.HangfireJobId,
                ["job_type"] = ambient?.JobTypeName
            }
        }, ex, ct);
    }

    public static IReadOnlyList<MediaItemDto> TryParsePartialFeed(
        string xml,
        string feedUrl,
        int maxItems,
        string? publicationName,
        bool includeFullContent)
    {
        var truncated = TruncateToCompleteItems(xml);
        if (string.IsNullOrWhiteSpace(truncated))
        {
            return [];
        }

        try
        {
            return ParseFeed(truncated, feedUrl, maxItems, publicationName, includeFullContent);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static string? TruncateToCompleteItems(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }

        var itemStart = xml.IndexOf("<item", StringComparison.OrdinalIgnoreCase);
        var itemEnd = xml.LastIndexOf("</item>", StringComparison.OrdinalIgnoreCase);
        if (itemStart >= 0 && itemEnd > itemStart)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel>"
                   + xml[itemStart..(itemEnd + "</item>".Length)]
                   + "</channel></rss>";
        }

        var entryStart = xml.IndexOf("<entry", StringComparison.OrdinalIgnoreCase);
        var entryEnd = xml.LastIndexOf("</entry>", StringComparison.OrdinalIgnoreCase);
        if (entryStart >= 0 && entryEnd > entryStart)
        {
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?><feed xmlns=\"http://www.w3.org/2005/Atom\">"
                   + xml[entryStart..(entryEnd + "</entry>".Length)]
                   + "</feed>";
        }

        return null;
    }

    private static IReadOnlyList<MediaItemDto> ParseFeed(
        string xml,
        string feedUrl,
        int maxItems,
        string? publicationName,
        bool includeFullContent)
    {
        var items = new List<MediaItemDto>();
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");
        nsmgr.AddNamespace("content", "http://purl.org/rss/1.0/modules/content/");
        nsmgr.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");

        var rssItems = doc.SelectNodes("//item");
        if (rssItems is not null && rssItems.Count > 0)
        {
            foreach (XmlNode node in rssItems)
            {
                if (items.Count >= maxItems)
                {
                    break;
                }

                var mapped = MapRssItem(node, nsmgr, feedUrl, publicationName, includeFullContent);
                if (mapped is not null)
                {
                    items.Add(mapped);
                }
            }

            return items;
        }

        var atomEntries = doc.SelectNodes("//atom:entry", nsmgr);
        if (atomEntries is not null)
        {
            foreach (XmlNode node in atomEntries)
            {
                if (items.Count >= maxItems)
                {
                    break;
                }

                var mapped = MapAtomEntry(node, nsmgr, feedUrl, publicationName, includeFullContent);
                if (mapped is not null)
                {
                    items.Add(mapped);
                }
            }
        }

        return items;
    }

    private static MediaItemDto? MapRssItem(
        XmlNode node,
        XmlNamespaceManager nsmgr,
        string feedUrl,
        string? publicationName,
        bool includeFullContent)
    {
        var title = node.SelectSingleNode("title")?.InnerText?.Trim() ?? string.Empty;
        var link = node.SelectSingleNode("link")?.InnerText?.Trim() ?? feedUrl;
        var guid = node.SelectSingleNode("guid")?.InnerText?.Trim() ?? link;
        var description = node.SelectSingleNode("description")?.InnerText?.Trim();
        var contentEncoded = node.SelectSingleNode("content:encoded", nsmgr)?.InnerText?.Trim()
            ?? node.SelectSingleNode("*[local-name()='encoded']")?.InnerText?.Trim();
        var author = node.SelectSingleNode("dc:creator", nsmgr)?.InnerText?.Trim()
            ?? node.SelectSingleNode("author")?.InnerText?.Trim();
        var pubDate = node.SelectSingleNode("pubDate")?.InnerText?.Trim();
        var enclosure = node.SelectSingleNode("enclosure")?.Attributes?["url"]?.Value;

        DateTimeOffset? publishedAt = null;
        if (!string.IsNullOrWhiteSpace(pubDate) && DateTimeOffset.TryParse(pubDate, out var parsed))
        {
            publishedAt = parsed.ToUniversalTime();
        }

        var fullText = includeFullContent
            ? contentEncoded ?? description
            : null;
        var summary = includeFullContent ? TruncateHtml(description, 500) : TruncateHtml(description, 500);

        return new MediaItemDto(
            guid,
            title,
            summary,
            link,
            enclosure,
            publishedAt,
            feedUrl,
            Author: author,
            Publication: publicationName,
            FullText: fullText);
    }

    private static MediaItemDto? MapAtomEntry(
        XmlNode node,
        XmlNamespaceManager nsmgr,
        string feedUrl,
        string? publicationName,
        bool includeFullContent)
    {
        var title = node.SelectSingleNode("atom:title", nsmgr)?.InnerText?.Trim() ?? string.Empty;
        var id = node.SelectSingleNode("atom:id", nsmgr)?.InnerText?.Trim() ?? title;
        var linkNode = node.SelectSingleNode("atom:link[@rel='alternate']", nsmgr)
                       ?? node.SelectSingleNode("atom:link", nsmgr);
        var link = linkNode?.Attributes?["href"]?.Value ?? feedUrl;
        var summary = node.SelectSingleNode("atom:summary", nsmgr)?.InnerText?.Trim();
        var content = node.SelectSingleNode("atom:content", nsmgr)?.InnerText?.Trim();
        var author = node.SelectSingleNode("atom:author/atom:name", nsmgr)?.InnerText?.Trim();
        var updated = node.SelectSingleNode("atom:updated", nsmgr)?.InnerText?.Trim()
                      ?? node.SelectSingleNode("atom:published", nsmgr)?.InnerText?.Trim();

        DateTimeOffset? publishedAt = null;
        if (!string.IsNullOrWhiteSpace(updated) && DateTimeOffset.TryParse(updated, out var parsed))
        {
            publishedAt = parsed.ToUniversalTime();
        }

        var fullText = includeFullContent ? content ?? summary : null;

        return new MediaItemDto(
            id,
            title,
            TruncateHtml(summary, 500),
            link,
            null,
            publishedAt,
            feedUrl,
            Author: author,
            Publication: publicationName,
            FullText: fullText);
    }

    private static string? TruncateHtml(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var stripped = System.Net.WebUtility.HtmlDecode(value).Trim();
        if (stripped.Length <= maxLength)
        {
            return stripped;
        }

        return stripped[..maxLength];
    }

    private static string MapFetchFailureReason(SafeHttpFailureKind kind) =>
        kind switch
        {
            SafeHttpFailureKind.Ssrf => "ssrf",
            SafeHttpFailureKind.HttpStatus => "non_200",
            SafeHttpFailureKind.ContentType => "invalid_xml",
            SafeHttpFailureKind.Oversized => "oversized",
            SafeHttpFailureKind.TooManyRedirects => "too_many_redirects",
            SafeHttpFailureKind.EmptyUrl => "empty_url",
            _ => "unavailable"
        };
}
