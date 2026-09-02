namespace BanterApp.Api.Integrations.Rss;

public static class RssUrlNormalizer
{
    public static string? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        return trimmed.EndsWith('/') && trimmed.Count(c => c == '/') > 3
            ? trimmed.TrimEnd('/')
            : trimmed;
    }

    public static bool EqualsUrl(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikeFeed(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return content.Contains("<rss", StringComparison.OrdinalIgnoreCase)
            || content.Contains("<feed", StringComparison.OrdinalIgnoreCase)
            || content.Contains("<rdf:RDF", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAbsoluteHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
