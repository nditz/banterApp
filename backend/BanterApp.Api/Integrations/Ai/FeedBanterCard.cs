namespace BanterApp.Api.Integrations.Ai;

/// <summary>Gen Z banter rewrite for a rolling feed card (headline + body + GIF mood).</summary>
public sealed record FeedBanterCard(
    string Title,
    string Body,
    string Mood,
    string? JokeLine = null);
