using System.Text;
using System.Text.RegularExpressions;
using BanterApp.Api.Common;

namespace BanterApp.Api.Integrations.Rss;

public static class RssFeedSlug
{
    private static readonly Regex NonSlug = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static string From(string kind, string name)
    {
        var prefix = string.IsNullOrWhiteSpace(kind) ? "feed" : kind.Trim().ToLowerInvariant();
        var slug = Slugify(name);
        if (string.IsNullOrEmpty(slug))
        {
            slug = "unnamed";
        }

        var combined = $"{prefix}-{slug}";
        return combined.Length <= StringLimits.RssFeedSlug
            ? combined
            : combined[..StringLimits.RssFeedSlug].TrimEnd('-');
    }

    public static string FromUrl(string kind, string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return From(kind, url);
        }

        var path = uri.AbsolutePath.Trim('/');
        var label = string.IsNullOrEmpty(path) ? uri.Host : $"{uri.Host}-{path}";
        return From(kind, label);
    }

    public static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return NonSlug.Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-").Trim('-');
    }
}
