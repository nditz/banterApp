namespace BanterApp.Api.Features.Studio;

public static class StudioContentCatalog
{
    public static readonly HashSet<string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "short",
        "podcast",
        "meme",
        "caption",
        "thread",
        "carousel",
        "commentary"
    };

    public static readonly HashSet<string> Tones = new(StringComparer.OrdinalIgnoreCase)
    {
        "funny",
        "ruthless",
        "analytical",
        "rant",
        "victory_lap",
        "self_roast",
        "pundit",
        "explainer"
    };

    public const string KindReceipt = "receipt";
    public const string KindVsPundit = "vs_pundit";
    public const string KindTrending = "trending";
    public const string KindProject = "project";

    public static string NormalizeType(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "short" : value.Trim().ToLowerInvariant();

    public static string NormalizeTone(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "explainer" : value.Trim().ToLowerInvariant();
}
