using System.Xml;
using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media.Dtos;
using BanterApp.Api.Services;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Integrations.Media;

public interface IRssFeedProvider
{
    Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
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

    public Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default) =>
        FetchFeedAsync(feedUrl, maxItems, publicationName: null, includeFullContent: false, cancellationToken);

    public async Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        string? publicationName,
        bool includeFullContent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return [];
        }

        try
        {
            var response = await _safeHttpClient.GetStringAsync(feedUrl, cancellationToken);
            if (response is null || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("RSS fetch failed or blocked for {Url}.", feedUrl);
                await TrackRssErrorAsync("non_200", feedUrl, (int?)response?.StatusCode, ssrfBlocked: response is null, ct: cancellationToken);
                return [];
            }

            return ParseFeed(response.Content, feedUrl, maxItems, publicationName, includeFullContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RSS fetch failed for {Url}.", feedUrl);
            await TrackRssExceptionAsync(ex, feedUrl, cancellationToken);
            return [];
        }
    }

    private async Task TrackRssErrorAsync(
        string reason,
        string feedUrl,
        int? statusCode,
        bool ssrfBlocked,
        CancellationToken ct)
    {
        var mapped = ProviderErrorMapper.MapRss(reason, statusCode, feedUrl, ssrfBlocked: ssrfBlocked);
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = mapped.Code,
            MessageSafe = mapped.SafeMessage,
            Severity = ssrfBlocked ? "warning" : "error",
            Provider = "rss",
            IsRetryable = mapped.IsRetryable,
            Metadata = mapped.Metadata
        }, ct);
    }

    private async Task TrackRssExceptionAsync(Exception ex, string feedUrl, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tracking = scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
        await tracking.TrackExceptionAsync(new ErrorTrackRequest
        {
            Source = "provider",
            ErrorCode = ErrorCodes.RssFetchError,
            MessageSafe = "We could not load this feed right now.",
            Severity = "error",
            Provider = "rss",
            IsRetryable = true,
            Metadata = new Dictionary<string, object?> { ["feed_url"] = feedUrl }
        }, ex, ct);
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
}
