namespace BanterApp.Api.Integrations.Ai;

/// <summary>ChatGPT pick for feed visuals — GIF mood tag, GIF search query, or DALL-E still.</summary>
public sealed record FeedVisualSuggestion(
    string Format,
    string? Mood,
    string? ImagePrompt,
    string? GifQuery = null)
{
    public bool IsGif =>
        string.Equals(Format, "gif", StringComparison.OrdinalIgnoreCase);

    public bool IsImage =>
        string.Equals(Format, "image", StringComparison.OrdinalIgnoreCase);
}
