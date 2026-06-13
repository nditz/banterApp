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
}
