using System.Text.Json;
using BanterApp.Api.Integrations.FootballBanter;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballBanterConfigValidatorTests
{
    [Fact]
    public void ValidateAndApplyDefaults_AppliesSafeDefaultsForMinimalConfig()
    {
        var config = new FootballBanterConfig();
        var result = FootballBanterConfigValidator.ValidateAndApplyDefaults(config);

        Assert.True(result.IsValid);
        Assert.Equal("1.0.0", result.Config.Version);
        Assert.Equal("https://www.googleapis.com/youtube/v3", result.Config.Sources.YouTube.ApiBaseUrl);
        Assert.Equal(7, result.Config.Banter.DefaultIntensity);
        Assert.NotEmpty(result.Config.ReviewRules.NeedsHumanReviewWhen);
    }

    [Fact]
    public void ValidateAndApplyDefaults_DisablesRssWhenFeedsMissing()
    {
        var config = new FootballBanterConfig
        {
            Sources = new FootballBanterSourcesConfig
            {
                Rss = new FootballBanterRssSourceConfig { Enabled = true, Feeds = [] }
            }
        };

        var result = FootballBanterConfigValidator.ValidateAndApplyDefaults(config);

        Assert.True(result.IsValid);
        Assert.False(result.Config.Sources.Rss.Enabled);
        Assert.Contains(result.Warnings, w => w.Contains("RSS ingest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LoadFromJson_InvalidJsonReturnsErrors()
    {
        var result = FootballBanterConfigLoader.LoadFromJson("{ not json");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void LoadFromContentRoot_LoadsCommittedConfig()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BanterApp.Api"));
        var result = FootballBanterConfigLoader.LoadFromContentRoot(contentRoot);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(4, result.Config.Sources.Rss.Feeds.Count);
        Assert.Contains(
            result.Config.Sources.Rss.Feeds,
            f => f.Url.Contains("bbci.co.uk", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildRssFeedSourceMap_UsesConfiguredSourceNames()
    {
        var config = new FootballBanterConfig
        {
            Sources = new FootballBanterSourcesConfig
            {
                Rss = new FootballBanterRssSourceConfig
                {
                    Feeds =
                    [
                        new FootballBanterRssFeedConfig
                        {
                            Url = "https://example.com/feed.xml",
                            SourceName = "Example Sport"
                        }
                    ]
                }
            }
        };

        var map = FootballBanterConfigProvider.BuildRssFeedSourceMap(config);

        Assert.Equal("Example Sport", map["https://example.com/feed.xml"]);
    }
}

public class FootballBanterOutputParserTests
{
    [Fact]
    public void TryParse_ValidJson_ReturnsOutput()
    {
        const string json = """
            {
              "headline": "No cap headline",
              "banter_summary": "Fun banter body",
              "meme_reactions": ["POV: chaos"],
              "gif_suggestions": ["Roy Keane angry"],
              "fan_reactions": ["NO CAP"],
              "confidence": 0.82,
              "source_name": "BBC Sport",
              "source_url": "https://bbc.co.uk/example",
              "pundit_name": "Gary Neville",
              "prediction": "England reach semis",
              "statement_type": "paraphrase",
              "needs_human_review": false
            }
            """;

        var output = FootballBanterOutputParser.TryParse(json);

        Assert.NotNull(output);
        Assert.Equal("BBC Sport", output!.SourceName);
        Assert.Equal("https://bbc.co.uk/example", output.SourceUrl);
        Assert.Equal(FootballBanterStatementType.Paraphrase, output.StatementType);
        Assert.Single(output.MemeReactions);
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsNull()
    {
        Assert.Null(FootballBanterOutputParser.TryParse("not-json"));
    }
}

public class FootballBanterEngineTests
{
    [Fact]
    public async Task GenerateAsync_PreservesSourceAttribution()
    {
        var config = new TestFootballBanterConfigProvider();
        var engine = new FootballBanterEngine(
            config,
            new StubFootballBanterContentGenerator(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FootballBanterEngine>.Instance);

        var output = await engine.GenerateAsync(new FootballBanterSourceInput
        {
            SourceType = "rss",
            SourceName = "BBC Sport",
            SourceUrl = "https://bbc.co.uk/article",
            SourceTitle = "England update",
            SourceText = "England are preparing for the World Cup with a strong squad and high expectations from fans."
        });

        Assert.Equal("BBC Sport", output.SourceName);
        Assert.Equal("https://bbc.co.uk/article", output.SourceUrl);
        Assert.False(string.IsNullOrWhiteSpace(output.Headline));
        Assert.False(string.IsNullOrWhiteSpace(output.BanterSummary));
    }

    [Fact]
    public async Task GenerateAsync_StubReturnsValidJsonShape()
    {
        var input = new FootballBanterSourceInput
        {
            SourceType = "youtube",
            SourceName = "Sky Sports",
            SourceUrl = "https://youtube.com/watch?v=abc",
            SourceTitle = "Pundit predictions",
            PunditName = "Jamie Carragher",
            SourceText = "Jamie Carragher thinks England can go deep if they stay compact defensively in the tournament."
        };

        var json = FootballBanterStubOutputBuilder.BuildJson(input);
        var output = FootballBanterOutputParser.TryParse(json);

        Assert.NotNull(output);
        Assert.Equal("Sky Sports", output!.SourceName);
        Assert.NotEmpty(output.GifSuggestions);
    }

    private sealed class TestFootballBanterConfigProvider : IFootballBanterConfigProvider
    {
        public FootballBanterConfig Config { get; } = new() { OpenAi = new FootballBanterOpenAiConfig { Enabled = false } };
        public string SystemPrompt { get; } = "test prompt";
        public IReadOnlyDictionary<string, string> RssFeedSourceNames { get; } = new Dictionary<string, string>();
        public bool IsValid => true;
        public IReadOnlyList<string> LoadErrors { get; } = [];
        public IReadOnlyList<string> LoadWarnings { get; } = [];
    }

    private sealed class StubFootballBanterContentGenerator : Integrations.Ai.IContentGenerator
    {
        public Task<bool> CanGenerateAsync(string? userId, bool isAnonymous, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<string> GenerateBanterAsync(string userPrediction, string actualResult, Integrations.Ai.BanterTone tone = Integrations.Ai.BanterTone.Friendly, string? userId = null, bool isAnonymous = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> GenerateAnalysisAsync(string userPrediction, Integrations.SportsData.Dtos.MatchStatisticsDto matchStats, string? userId = null, bool isAnonymous = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> GenerateMemeCaptionAsync(string context, string? userId = null, bool isAnonymous = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> GenerateVideoScriptAsync(Integrations.Ai.VideoScriptFormat format, Integrations.Ai.VideoScriptDuration duration, string context, string? userId = null, bool isAnonymous = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string> GenerateNewsReactionAsync(string headline, string summary, string? category = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<string?> GenerateReactionImageUrlAsync(string headline, string reactionText, string? category = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<Integrations.Ai.FeedVisualSuggestion> SuggestFeedVisualAsync(string headline, string reactionText, string? category = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Integrations.Ai.FeedVisualSuggestion("gif", "news", null));

        public Task<Integrations.Ai.FeedBanterCard> GenerateFeedBanterCardAsync(string headline, string summary, string? category = null, string? author = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Integrations.Ai.FeedBanterCard(headline, summary, "news"));

        public Task<string> GenerateFootballBanterJsonAsync(
            FootballBanterSourceInput input,
            string systemPrompt,
            FootballBanterOpenAiConfig openAiConfig,
            int banterIntensity,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FootballBanterStubOutputBuilder.BuildJson(input));
    }
}
