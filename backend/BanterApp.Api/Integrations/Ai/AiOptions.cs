namespace BanterApp.Api.Integrations.Ai;

/// <summary>
/// AI provider configuration for banter generation, news reactions, and broadcast scripts.
/// Phase 2: swap <see cref="StubContentGenerator"/> for OpenAI / Anthropic behind <see cref="IContentGenerator"/>.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>stub | openai | anthropic</summary>
    public string Provider { get; set; } = "stub";

    public string? ApiKey { get; set; }

    /// <summary>Optional override (Azure OpenAI, local Ollama, etc.).</summary>
    public string? BaseUrl { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public int MaxTokens { get; set; } = 512;

    public double Temperature { get; set; } = 0.85;

    public bool Enabled { get; set; } = true;

    /// <summary>System prompt for news/match reaction posts in the rolling feed.</summary>
    public string NewsReactionSystemPrompt { get; set; } =
        "You are a witty football pundit on a banter app. React to the news in 2-3 sentences " +
        "like a TV journalist or online personality — opinionated, fun, PG-rated. " +
        "Never encourage gambling. Use casual fan language.";

    /// <summary>Max AI generations per anonymous session (registered users unlimited).</summary>
    public int AnonymousGenerationLimit { get; set; } = 3;

    /// <summary>Generate DALL-E images for feed reactions and meme banter.</summary>
    public bool EnableImageGeneration { get; set; } = true;

    public string ImageModel { get; set; } = "dall-e-3";

    /// <summary>dall-e-3: 1024x1024, 1024x1792, or 1792x1024.</summary>
    public string ImageSize { get; set; } = "1024x1024";

    /// <summary>Prompt prefix for meme / GIF-style still frames in the feed.</summary>
    public string MemeImagePrompt { get; set; } =
        "Funny football banter meme illustration, bold expressive cartoon style, " +
        "single frozen frame like a viral sports GIF, PG-rated, no text in image.";

    /// <summary>System prompt for ChatGPT to pick GIF mood vs DALL-E image for feed cards.</summary>
    public string FeedVisualSystemPrompt { get; set; } =
        "You pick visuals for a football banter app feed. Reply ONLY with JSON: " +
        "{\"format\":\"gif\"|\"image\",\"mood\":\"celebrate|hype|debate|shock|chaos|facepalm|miss|roast|trophy|news|pundit\",\"imagePrompt\":\"...\"}. " +
        "Use format gif for reactions, hot takes, roasts, and wins. Use format image for news desk scenes. " +
        "When format is gif, set mood and leave imagePrompt empty. When format is image, set imagePrompt (short scene description) and leave mood empty.";
}
