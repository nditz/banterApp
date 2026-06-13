using System.Xml;
using BanterApp.Api.Integrations.Media.Dtos;
using Microsoft.Extensions.Logging;

namespace BanterApp.Api.Integrations.Media;

public interface IRssFeedProvider
{
    Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RSS/Atom feed parser for podcasts and sports websites. Does not crawl HTML pages.
/// </summary>
public sealed class RssFeedProvider : IRssFeedProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RssFeedProvider> _logger;

    public RssFeedProvider(HttpClient httpClient, ILogger<RssFeedProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaItemDto>> FetchFeedAsync(
        string feedUrl,
        int maxItems,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return [];
        }

        try
        {
            using var response = await _httpClient.GetAsync(feedUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("RSS fetch failed for {Url}: {Status}", feedUrl, (int)response.StatusCode);
                return [];
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseFeed(xml, feedUrl, maxItems);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RSS fetch failed for {Url}.", feedUrl);
            return [];
        }
    }

    private static IReadOnlyList<MediaItemDto> ParseFeed(string xml, string feedUrl, int maxItems)
    {
        var items = new List<MediaItemDto>();
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");

        var rssItems = doc.SelectNodes("//item");
        if (rssItems is not null && rssItems.Count > 0)
        {
            foreach (XmlNode node in rssItems)
            {
                if (items.Count >= maxItems)
                {
                    break;
                }

                var mapped = MapRssItem(node, feedUrl);
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

                var mapped = MapAtomEntry(node, nsmgr, feedUrl);
                if (mapped is not null)
                {
                    items.Add(mapped);
                }
            }
        }

        return items;
    }

    private static MediaItemDto? MapRssItem(XmlNode node, string feedUrl)
    {
        var title = node.SelectSingleNode("title")?.InnerText?.Trim() ?? string.Empty;
        var link = node.SelectSingleNode("link")?.InnerText?.Trim() ?? feedUrl;
        var guid = node.SelectSingleNode("guid")?.InnerText?.Trim() ?? link;
        var description = node.SelectSingleNode("description")?.InnerText?.Trim();
        var pubDate = node.SelectSingleNode("pubDate")?.InnerText?.Trim();
        var enclosure = node.SelectSingleNode("enclosure")?.Attributes?["url"]?.Value;

        DateTimeOffset? publishedAt = null;
        if (!string.IsNullOrWhiteSpace(pubDate) && DateTimeOffset.TryParse(pubDate, out var parsed))
        {
            publishedAt = parsed;
        }

        return new MediaItemDto(guid, title, Truncate(description, 500), link, enclosure, publishedAt, feedUrl);
    }

    private static MediaItemDto? MapAtomEntry(XmlNode node, XmlNamespaceManager nsmgr, string feedUrl)
    {
        var title = node.SelectSingleNode("atom:title", nsmgr)?.InnerText?.Trim() ?? string.Empty;
        var id = node.SelectSingleNode("atom:id", nsmgr)?.InnerText?.Trim() ?? title;
        var linkNode = node.SelectSingleNode("atom:link[@rel='alternate']", nsmgr)
                       ?? node.SelectSingleNode("atom:link", nsmgr);
        var link = linkNode?.Attributes?["href"]?.Value ?? feedUrl;
        var summary = node.SelectSingleNode("atom:summary", nsmgr)?.InnerText?.Trim()
                      ?? node.SelectSingleNode("atom:content", nsmgr)?.InnerText?.Trim();
        var updated = node.SelectSingleNode("atom:updated", nsmgr)?.InnerText?.Trim()
                      ?? node.SelectSingleNode("atom:published", nsmgr)?.InnerText?.Trim();

        DateTimeOffset? publishedAt = null;
        if (!string.IsNullOrWhiteSpace(updated) && DateTimeOffset.TryParse(updated, out var parsed))
        {
            publishedAt = parsed;
        }

        return new MediaItemDto(id, title, Truncate(summary, 500), link, null, publishedAt, feedUrl);
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
