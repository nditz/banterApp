using System.Text.RegularExpressions;

namespace BanterApp.Api.Common;

public static partial class HtmlSanitizer
{
    private static readonly Regex TagRegex = TagPattern();
    private static readonly Regex ScriptStyleRegex = ScriptStylePattern();
    private static readonly Regex EventHandlerRegex = EventHandlerPattern();
    private static readonly Regex JavascriptUrlRegex = JavascriptUrlPattern();

    public static string SanitizePlainText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        var decoded = System.Net.WebUtility.HtmlDecode(value);
        decoded = ScriptStyleRegex.Replace(decoded, string.Empty);
        decoded = TagRegex.Replace(decoded, string.Empty);
        decoded = EventHandlerRegex.Replace(decoded, string.Empty);
        decoded = JavascriptUrlRegex.Replace(decoded, string.Empty);
        return decoded.Trim();
    }

    public static bool ContainsDangerousMarkup(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TagRegex.IsMatch(value) ||
               ScriptStyleRegex.IsMatch(value) ||
               EventHandlerRegex.IsMatch(value) ||
               JavascriptUrlRegex.IsMatch(value);
    }

    [GeneratedRegex("<(script|style)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStylePattern();

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\bon\w+\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerPattern();

    [GeneratedRegex(@"javascript\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptUrlPattern();
}
