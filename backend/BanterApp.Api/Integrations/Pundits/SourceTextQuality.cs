namespace BanterApp.Api.Integrations.Pundits;

/// <summary>
/// Grounding gate for pundit extraction. Title/description fallbacks are not
/// transcripts and must not be treated as source text.
/// </summary>
public static class SourceTextQuality
{
    public const string IncompleteTranscriptMessage =
        "Transcript incomplete; captions were not available.";

    public static bool IsUsable(string? text, int minLength) =>
        !string.IsNullOrWhiteSpace(text) &&
        text.Trim().Length >= Math.Max(1, minLength);

    public static bool IsTitleDescriptionFallback(
        string? title,
        string? description,
        string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return true;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
        {
            parts.Add(title.Trim());
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description.Trim());
        }

        if (parts.Count == 0)
        {
            return true;
        }

        var fallback = string.Join("\n\n", parts);
        return string.Equals(rawText.Trim(), fallback, StringComparison.Ordinal);
    }
}
