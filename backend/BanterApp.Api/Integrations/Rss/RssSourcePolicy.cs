namespace BanterApp.Api.Integrations.Rss;

/// <summary>
/// Hosts we must not fetch. BBC is out of scope for ingest — do not scrape it.
/// </summary>
public static class RssSourcePolicy
{
    private static readonly string[] DisallowedHostSuffixes =
    [
        "bbc.co.uk",
        "bbci.co.uk",
        "bbc.com"
    ];

    public static bool IsDisallowed(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        var host = uri.Host.Trim().TrimEnd('.').ToLowerInvariant();
        return DisallowedHostSuffixes.Any(suffix =>
            host.Equals(suffix, StringComparison.Ordinal) ||
            host.EndsWith("." + suffix, StringComparison.Ordinal));
    }
}
