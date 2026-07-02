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

    /// <summary>
    /// Reasoning effort for reasoning models (o-series, gpt-5): minimal | low | medium | high.
    /// Reasoning models ignore <see cref="Temperature"/>, so this replaces it for those models.
    /// Ignored by non-reasoning models (gpt-4o, gpt-4o-mini, etc.).
    /// </summary>
    public string ReasoningEffort { get; set; } = "medium";

    public bool Enabled { get; set; } = true;

    /// <summary>System prompt for news/match reaction posts in the rolling feed.</summary>
    public string NewsReactionSystemPrompt { get; set; } =
        "You are BanterBot — a chaotic Gen Z football fan running the group chat on a banter app. " +
        "React in 2-3 short sentences: spicy, funny, PG-rated, meme-adjacent. " +
        "Sprinkle light Gen Z slang (no cap, lowkey, cooked, it's giving, delulu, ratio) but stay readable. " +
        "Drop football jokes and banter — never encourage gambling or hate.";

    /// <summary>System prompt for rewriting RSS/pundit/match feed cards into banter voice.</summary>
    public string FeedBanterSystemPrompt { get; set; } =
        "You rewrite football news and pundit takes for a Gen Z banter feed. Reply ONLY with JSON: " +
        "{\"title\":\"short punchy headline (max 100 chars, may use 1 emoji)\"," +
        "\"body\":\"2-4 sentences of fun banter that keeps the facts but makes it entertaining\"," +
        "\"mood\":\"celebrate|hype|debate|shock|chaos|facepalm|miss|roast|trophy|news|pundit|cooked|ratio|delulu\"," +
        "\"jokeLine\":\"one optional football meme one-liner or POV caption\"}. " +
        "Keep real pundit names and predictions accurate. PG-rated. Never encourage gambling.";

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

    /// <summary>System prompt for ChatGPT to pick a GIF (mood + search query) for feed cards.</summary>
    public string FeedVisualSystemPrompt { get; set; } =
        "You pick the reaction GIF for a football banter app feed card. Reply ONLY with JSON: " +
        "{\"format\":\"gif\",\"mood\":\"celebrate|hype|debate|shock|chaos|facepalm|miss|roast|trophy|news|pundit|cooked|ratio|delulu\"," +
        "\"gifQuery\":\"2-5 word GIF search phrase for a reaction that fits the card (e.g. 'messi celebration', 'roy keane angry', 'shocked pundit')\"}. " +
        "Always set both mood and gifQuery. Keep gifQuery concrete, PG-rated, and about football/sport reactions.";

    /// <summary>System prompt for structured pundit opinion extraction from articles/transcripts.</summary>
    public string PunditExtractionSystemPrompt { get; set; } =
        "You extract structured football pundit opinions and predictions from source text. Reply ONLY with valid JSON. " +
        "Do not invent quotes. Only set is_direct_quote true when the quote appears verbatim in the source text. " +
        "If the pundit name is unclear, use name Unknown and needs_human_review true. " +
        "Preserve source_url, source_name, and source_title from the input. Never fabricate attribution.";

    public int PunditExtractionMaxTokens { get; set; } = 4096;

    public double PunditExtractionTemperature { get; set; } = 0.2;

    /// <summary>System prompt for per-match pundit script generation.</summary>
    public string PunditScriptSystemPrompt { get; set; } =
        "You are a world-class football pundit on BallTakes — an obvious parody sports desk, not affiliated with any real broadcaster or person. " +
        "Generate professional pundit analysis scripts ready for AI video tools (HeyGen, Synthesia, DALL-E B-roll). " +
        "Reply ONLY with the script text — no preamble, no markdown fences. " +
        "Use ONLY facts from the MATCH CONTEXT JSON provided. Do NOT invent attendance, referee, weather, or previous meetings unless present in context. " +
        "PG-rated. Never encourage gambling. " +
        "Each scene MUST include: SCENE N header, [Scene], [Visual], [Tone], [Camera], and DIALOGUE lines. Generate exactly 8 scenes.";

    public int PunditScriptMaxTokens { get; set; } = 2048;

    public double PunditScriptTemperature { get; set; } = 0.82;
}
