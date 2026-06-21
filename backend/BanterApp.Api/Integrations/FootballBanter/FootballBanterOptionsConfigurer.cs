using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Pundits;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.FootballBanter;

public sealed class FootballBanterPunditIngestPostConfigurer : IPostConfigureOptions<PunditIngestOptions>
{
    private readonly IFootballBanterConfigProvider _config;

    public FootballBanterPunditIngestPostConfigurer(IFootballBanterConfigProvider config)
    {
        _config = config;
    }

    public void PostConfigure(string? name, PunditIngestOptions options)
    {
        if (!_config.IsValid)
        {
            return;
        }

        var cfg = _config.Config;
        var rssEnabled = cfg.Sources.Rss.Enabled;
        var youtubeEnabled = cfg.Sources.YouTube.Enabled;

        if (!rssEnabled && !youtubeEnabled)
        {
            options.Enabled = false;
            return;
        }

        if (rssEnabled && cfg.Sources.Rss.Feeds.Count > 0)
        {
            options.RssFeedUrls = cfg.Sources.Rss.Feeds
                .Select(f => f.Url.Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToArray();
        }

        if (youtubeEnabled && cfg.Sources.YouTube.SearchTerms.Count > 0)
        {
            options.YouTubeSearchQueries = cfg.Sources.YouTube.SearchTerms
                .Select(q => q.Trim())
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .ToArray();
        }

        if (cfg.Sources.YouTube.MaxResultsPerSearch > 0)
        {
            options.MaxItemsPerSource = cfg.Sources.YouTube.MaxResultsPerSearch;
        }
    }
}

public sealed class FootballBanterYouTubePostConfigurer : IPostConfigureOptions<Media.YouTubeOptions>
{
    private readonly IFootballBanterConfigProvider _config;

    public FootballBanterYouTubePostConfigurer(IFootballBanterConfigProvider config)
    {
        _config = config;
    }

    public void PostConfigure(string? name, Media.YouTubeOptions options)
    {
        if (!_config.IsValid)
        {
            return;
        }

        var youtube = _config.Config.Sources.YouTube;
        if (!string.IsNullOrWhiteSpace(youtube.ApiBaseUrl))
        {
            options.BaseUrl = youtube.ApiBaseUrl.Trim();
        }

        if (youtube.SearchTerms.Count > 0)
        {
            options.DefaultSearchTerms = youtube.SearchTerms.ToArray();
        }
    }
}

public sealed class FootballBanterBackgroundJobsPostConfigurer : IPostConfigureOptions<BackgroundJobsOptions>
{
    private readonly IFootballBanterConfigProvider _config;

    public FootballBanterBackgroundJobsPostConfigurer(IFootballBanterConfigProvider config)
    {
        _config = config;
    }

    public void PostConfigure(string? name, BackgroundJobsOptions options)
    {
        if (!_config.IsValid)
        {
            return;
        }

        var rssMinutes = _config.Config.Sources.Rss.SyncIntervalMinutes;
        if (rssMinutes > 0)
        {
            options.RssOpinionSyncIntervalMinutes = rssMinutes;
        }

        var youtubeMinutes = _config.Config.Sources.YouTube.SyncIntervalMinutes;
        if (youtubeMinutes > 0)
        {
            options.YouTubeSearchSyncIntervalMinutes = youtubeMinutes;
        }
    }
}

public sealed class FootballBanterAiPostConfigurer : IPostConfigureOptions<AiOptions>
{
    private readonly IFootballBanterConfigProvider _config;

    public FootballBanterAiPostConfigurer(IFootballBanterConfigProvider config)
    {
        _config = config;
    }

    public void PostConfigure(string? name, AiOptions options)
    {
        if (!_config.IsValid || !_config.Config.OpenAi.Enabled)
        {
            return;
        }

        var openAi = _config.Config.OpenAi;
        if (!string.IsNullOrWhiteSpace(openAi.Model))
        {
            options.Model = openAi.Model.Trim();
        }

        options.Temperature = openAi.Temperature;
        if (openAi.MaxOutputTokens > 0)
        {
            options.MaxTokens = openAi.MaxOutputTokens;
        }

        if (!string.IsNullOrWhiteSpace(_config.SystemPrompt))
        {
            options.FeedBanterSystemPrompt = _config.SystemPrompt;
        }
    }
}
