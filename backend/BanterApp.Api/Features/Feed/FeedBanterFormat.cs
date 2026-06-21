namespace BanterApp.Api.Features.Feed;

using BanterApp.Api.Common;

/// <summary>
/// Tracks in-place feed copy that has been rewritten into banter voice (no extra DB column).
/// </summary>
public static class FeedBanterFormat
{
    public const string Marker = "[BANTER]";

    public static bool IsBanterized(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        text.StartsWith(Marker, StringComparison.Ordinal);

    public static string Mark(string text) => $"{Marker}{HtmlSanitizer.SanitizePlainText(text)}";

    public static string Strip(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var value = text.StartsWith(Marker, StringComparison.Ordinal)
            ? text[Marker.Length..]
            : text;

        return HtmlSanitizer.SanitizePlainText(value);
    }
}
