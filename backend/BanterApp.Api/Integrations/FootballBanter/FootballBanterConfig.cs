using System.Text.Json.Serialization;

namespace BanterApp.Api.Integrations.FootballBanter;

public sealed class FootballBanterConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("feature")]
    public string Feature { get; set; } = "football_banter_engine";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sources")]
    public FootballBanterSourcesConfig Sources { get; set; } = new();

    [JsonPropertyName("openai")]
    public FootballBanterOpenAiConfig OpenAi { get; set; } = new();

    [JsonPropertyName("content_rules")]
    public FootballBanterContentRulesConfig ContentRules { get; set; } = new();

    [JsonPropertyName("banter")]
    public FootballBanterStyleConfig Banter { get; set; } = new();

    [JsonPropertyName("review_rules")]
    public FootballBanterReviewRulesConfig ReviewRules { get; set; } = new();
}

public sealed class FootballBanterSourcesConfig
{
    [JsonPropertyName("youtube")]
    public FootballBanterYouTubeSourceConfig YouTube { get; set; } = new();

    [JsonPropertyName("rss")]
    public FootballBanterRssSourceConfig Rss { get; set; } = new();
}

public sealed class FootballBanterYouTubeSourceConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("api_base_url")]
    public string ApiBaseUrl { get; set; } = "https://www.googleapis.com/youtube/v3";

    [JsonPropertyName("search_terms")]
    public List<string> SearchTerms { get; set; } = [];

    [JsonPropertyName("max_results_per_search")]
    public int MaxResultsPerSearch { get; set; } = 25;

    [JsonPropertyName("sync_interval_minutes")]
    public int SyncIntervalMinutes { get; set; } = 180;

    [JsonPropertyName("transcripts")]
    public FootballBanterTranscriptConfig Transcripts { get; set; } = new();
}

public sealed class FootballBanterTranscriptConfig
{
    [JsonPropertyName("prefer_official_captions")]
    public bool PreferOfficialCaptions { get; set; } = true;

    [JsonPropertyName("allow_fallback_to_description")]
    public bool AllowFallbackToDescription { get; set; } = true;

    [JsonPropertyName("allow_fallback_to_openai_enrichment")]
    public bool AllowFallbackToOpenAiEnrichment { get; set; } = true;

    [JsonPropertyName("requires_source_attribution")]
    public bool RequiresSourceAttribution { get; set; } = true;
}

public sealed class FootballBanterRssSourceConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("sync_interval_minutes")]
    public int SyncIntervalMinutes { get; set; } = 30;

    [JsonPropertyName("dedupe_strategy")]
    public List<string> DedupeStrategy { get; set; } = ["guid", "link", "content_hash"];

    [JsonPropertyName("feeds")]
    public List<FootballBanterRssFeedConfig> Feeds { get; set; } = [];
}

public sealed class FootballBanterRssFeedConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("source_name")]
    public string SourceName { get; set; } = string.Empty;
}

public sealed class FootballBanterOpenAiConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-4o-mini";

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.8;

    [JsonPropertyName("max_output_tokens")]
    public int MaxOutputTokens { get; set; } = 1800;

    [JsonPropertyName("pipeline_roles")]
    public FootballBanterPipelineRolesConfig PipelineRoles { get; set; } = new();
}

public sealed class FootballBanterPipelineRolesConfig
{
    [JsonPropertyName("extractor")]
    public FootballBanterRoleConfig Extractor { get; set; } = new();

    [JsonPropertyName("banter_engine")]
    public FootballBanterRoleConfig BanterEngine { get; set; } = new();
}

public sealed class FootballBanterRoleConfig
{
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    [JsonPropertyName("must_not_invent_quotes")]
    public bool MustNotInventQuotes { get; set; } = true;

    [JsonPropertyName("must_flag_uncertainty")]
    public bool MustFlagUncertainty { get; set; } = true;

    [JsonPropertyName("must_preserve_source_attribution")]
    public bool MustPreserveSourceAttribution { get; set; } = true;

    [JsonPropertyName("must_not_create_fake_claims")]
    public bool MustNotCreateFakeClaims { get; set; } = true;
}

public sealed class FootballBanterContentRulesConfig
{
    [JsonPropertyName("quote_policy")]
    public FootballBanterQuotePolicyConfig QuotePolicy { get; set; } = new();

    [JsonPropertyName("safety")]
    public FootballBanterSafetyConfig Safety { get; set; } = new();
}

public sealed class FootballBanterQuotePolicyConfig
{
    [JsonPropertyName("allow_short_quotes")]
    public bool AllowShortQuotes { get; set; } = true;

    [JsonPropertyName("avoid_long_copyrighted_excerpts")]
    public bool AvoidLongCopyrightedExcerpts { get; set; } = true;

    [JsonPropertyName("always_include_source_url")]
    public bool AlwaysIncludeSourceUrl { get; set; } = true;

    [JsonPropertyName("always_label_statement_type")]
    public bool AlwaysLabelStatementType { get; set; } = true;

    [JsonPropertyName("statement_types")]
    public List<string> StatementTypes { get; set; } =
    [
        "direct_quote",
        "paraphrase",
        "ai_summary",
        "inferred_prediction"
    ];
}

public sealed class FootballBanterSafetyConfig
{
    [JsonPropertyName("no_hate_speech")]
    public bool NoHateSpeech { get; set; } = true;

    [JsonPropertyName("no_harassment")]
    public bool NoHarassment { get; set; } = true;

    [JsonPropertyName("no_fake_endorsements")]
    public bool NoFakeEndorsements { get; set; } = true;

    [JsonPropertyName("no_claimed_affiliation_without_permission")]
    public bool NoClaimedAffiliationWithoutPermission { get; set; } = true;

    [JsonPropertyName("keep_banter_football_focused")]
    public bool KeepBanterFootballFocused { get; set; } = true;
}

public sealed class FootballBanterStyleConfig
{
    [JsonPropertyName("default_intensity")]
    public int DefaultIntensity { get; set; } = 7;

    [JsonPropertyName("allowed_intensity_range")]
    public List<int> AllowedIntensityRange { get; set; } = [1, 10];

    [JsonPropertyName("tone")]
    public List<string> Tone { get; set; } = [];
}

public sealed class FootballBanterReviewRulesConfig
{
    [JsonPropertyName("needs_human_review_when")]
    public List<string> NeedsHumanReviewWhen { get; set; } = [];
}
