using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BanterApp.Api.Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBanterIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BackgroundJobsOptions>(
            configuration.GetSection(BackgroundJobsOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<NewsIngestOptions>(
            configuration.GetSection(NewsIngestOptions.SectionName));
        services.Configure<NewsOptions>(configuration.GetSection(NewsOptions.SectionName));
        services.Configure<SportsDataOptions>(
            configuration.GetSection(SportsDataOptions.SectionName));
        services.Configure<SportmonksOptions>(
            configuration.GetSection(SportmonksOptions.SectionName));
        services.Configure<FootballDataOptions>(
            configuration.GetSection(FootballDataOptions.SectionName));
        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.Configure<MediaIngestOptions>(configuration.GetSection(MediaIngestOptions.SectionName));

        var sportsProvider = configuration["SportsData:Provider"]?.Trim().ToLowerInvariant() ?? "mock";
        var sportsApiKey = configuration["SportsData:ApiKey"];

        services.Configure<SportsDataOptions>(options =>
        {
            options.Provider = sportsProvider;
            if (!string.IsNullOrWhiteSpace(sportsApiKey))
            {
                options.ApiKey = sportsApiKey;
            }
        });

        switch (sportsProvider)
        {
            case "apifootball":
                services.AddHttpClient<ApiFootballHttpClient>();
                services.AddTransient<ISportsDataProvider, ApiFootballProvider>();
                services.AddTransient<ISportsDataEnrichment, ApiFootballProvider>();
                break;

            case "mock":
            default:
                if (sportsProvider != "mock")
                {
                    Console.Error.WriteLine(
                        $"Unknown SportsData:Provider '{sportsProvider}'; falling back to mock.");
                }

                services.TryAddSingleton<ISportsDataProvider, MockSportsDataProvider>();
                services.TryAddSingleton<ISportsDataEnrichment, MockSportsDataProvider>();
                break;
        }

        services.AddHttpClient<ISportsDataFallbackProvider, SportmonksProvider>();
        services.AddHttpClient<ISportsDataFallbackProvider, FootballDataProvider>();

        var newsApiKey = configuration["News:ApiKey"];
        if (!string.IsNullOrWhiteSpace(newsApiKey))
        {
            services.AddHttpClient<INewsProvider, NewsApiProvider>();
        }
        else
        {
            services.TryAddSingleton<INewsProvider, MockNewsProvider>();
        }

        services.AddHttpClient<IYouTubeProvider, YouTubeProvider>();
        services.AddHttpClient<IRssFeedProvider, RssFeedProvider>();

        var aiProvider = configuration["Ai:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        var aiApiKey = configuration["Ai:ApiKey"];
        if ((aiProvider is "openai" or "chatgpt") && !string.IsNullOrWhiteSpace(aiApiKey))
        {
            services.AddHttpClient<IContentGenerator, OpenAiContentGenerator>();
        }
        else
        {
            if (aiProvider is "openai" or "chatgpt")
            {
                Console.Error.WriteLine("Ai:Provider is openai but Ai:ApiKey is missing; using stub content generator.");
            }

            services.TryAddSingleton<IContentGenerator, StubContentGenerator>();
        }
        services.AddScoped<SyncRunTracker>();
        services.AddScoped<ScoreSyncJob>();
        services.AddScoped<StandingsSyncJob>();
        services.AddScoped<MatchDetailsSyncJob>();
        services.AddScoped<NewsIngestJob>();
        services.AddScoped<MediaIngestJob>();
        services.AddScoped<AiReactionJob>();

        return services;
    }
}
