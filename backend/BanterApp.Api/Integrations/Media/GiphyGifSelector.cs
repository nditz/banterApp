namespace BanterApp.Api.Integrations.Media;

public sealed record GiphyGifHit(string Id, string Url);

/// <summary>
/// Giphy identity helpers. CDN hosts rotate (<c>media1</c> vs <c>media4</c>), so uniqueness
/// is tracked by media ID rather than the full URL.
/// </summary>
public static class GiphyGifSelector
{
    public const int MaxOffset = 4999;
    public const int MaxRandomOffset = 1500;
    public const int MaxQueryLength = 50;
    public const int RandomAttempts = 3;
    public const int SearchAttempts = 2;

    public static string TruncateQuery(string query)
    {
        var trimmed = query.Trim();
        return trimmed.Length <= MaxQueryLength
            ? trimmed
            : trimmed[..MaxQueryLength].Trim();
    }

    public static string Identity(string? jsonId, string url)
    {
        if (!string.IsNullOrWhiteSpace(jsonId) && jsonId.Length is >= 4 and <= 64)
        {
            return jsonId.Trim();
        }

        return FromUrl(url) ?? url;
    }

    public static string? FromUrl(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!uri.Host.Contains("giphy.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var last = segments[^1];
        var candidate = last.Contains('.', StringComparison.Ordinal) && segments.Length >= 2
            ? segments[^2]
            : Path.GetFileNameWithoutExtension(last);

        return string.IsNullOrWhiteSpace(candidate) || candidate.Length < 4 || candidate.Length > 64
            ? null
            : candidate;
    }

    public static void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    public static int RandomSearchOffset() =>
        Random.Shared.Next(0, MaxRandomOffset + 1);
}
