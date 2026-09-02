using BanterApp.Api.Features.Feed;
using BanterApp.Api.Features.Matches;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Common;
using BanterApp.Api.Integrations.FootballBanter;
using BanterApp.Api.Integrations.FootballReference;
using BanterApp.Api.Integrations.FootballReference.Jobs;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.Pundits;
using BanterApp.Api.Integrations.Rss;
using BanterApp.Api.Integrations.SportsData;
using BanterApp.Api.Integrations.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBanterIntegrations(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
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
        services.Configure<FootballReferenceDataOptions>(
            configuration.GetSection(FootballReferenceDataOptions.SectionName));
        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.Configure<MediaIngestOptions>(configuration.GetSection(MediaIngestOptions.SectionName));
        services.Configure<PunditIngestOptions>(configuration.GetSection(PunditIngestOptions.SectionName));
        services.Configure<ProcessingOptions>(configuration.GetSection(ProcessingOptions.SectionName));
        services.Configure<SourceWeightsOptions>(configuration.GetSection(SourceWeightsOptions.SectionName));
        services.Configure<ReactionGifOptions>(configuration.GetSection(ReactionGifOptions.SectionName));

        services.AddSingleton<IFootballBanterConfigProvider>(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var logger = sp.GetRequiredService<ILogger<FootballBanterConfigProvider>>();
            return FootballBanterConfigProvider.Create(env.ContentRootPath, logger);
        });
        services.AddScoped<IFootballBanterEngine, FootballBanterEngine>();
        services.AddSingleton<IPostConfigureOptions<PunditIngestOptions>, FootballBanterPunditIngestPostConfigurer>();
        services.AddSingleton<IPostConfigureOptions<YouTubeOptions>, FootballBanterYouTubePostConfigurer>();
        services.AddSingleton<IPostConfigureOptions<BackgroundJobsOptions>, FootballBanterBackgroundJobsPostConfigurer>();
        services.AddSingleton<IPostConfigureOptions<AiOptions>, FootballBanterAiPostConfigurer>();

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

        services.AddHttpClient<ApiFootballHttpClient>();
        services.AddScoped<ApiSportsReferenceProvider>();
        services.AddHttpClient<SportmonksReferenceProvider>();
        services.AddScoped<GoogleReferenceProviderStub>();
        services.AddScoped<NoOpReferenceProvider>();
        services.AddScoped<FootballReferenceDataProviderFactory>();
        services.AddScoped<ReferenceDataUpsertService>();
        services.AddScoped<FootballCountriesSyncJob>();
        services.AddScoped<FootballPlayersSyncJob>();
        services.AddScoped<FootballPlayerStatsSyncJob>();
        services.AddScoped<FootballTopScorersSyncJob>();
        services.AddScoped<FootballTopAssistsSyncJob>();
        services.AddScoped<FootballReferenceFullSyncJob>();

        if (environment?.IsEnvironment("Testing") == true)
        {
            services.TryAddSingleton<INewsProvider, MockNewsProvider>();
        }
        else
        {
            services.AddHttpClient<NewsApiProvider>();
            services.AddSingleton<RssNewsProvider>();
            services.AddSingleton<INewsProvider, CompositeNewsProvider>();
        }

        services.AddHttpClient<IYouTubeProvider, YouTubeProvider>();
        services.AddSingleton<IRssFeedCatalogSeed, FileRssFeedCatalogSeed>();
        services.AddSingleton<IRssFeedProvider, RssFeedProvider>();
        services.AddScoped<IRssFeedCatalog, RssFeedCatalogService>();
        services.AddScoped<RssFeedResolver>();
        services.AddScoped<IArticleContentFetcher, ArticleContentFetcher>();
        services.AddHttpClient<IYouTubeTranscriptProvider, YouTubeTranscriptProvider>();

        var aiProvider = configuration["Ai:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        var aiApiKey = configuration["Ai:ApiKey"];
        if ((aiProvider is "openai" or "chatgpt") && !string.IsNullOrWhiteSpace(aiApiKey))
        {
            services.AddHttpClient<IContentGenerator, OpenAiContentGenerator>();
            services.AddHttpClient<IPunditOpinionExtractor, OpenAiPunditOpinionExtractor>();
        }
        else
        {
            if (aiProvider is "openai" or "chatgpt")
            {
                Console.Error.WriteLine("Ai:Provider is openai but Ai:ApiKey is missing; using stub content generator.");
            }

            services.TryAddSingleton<IContentGenerator, StubContentGenerator>();
            services.TryAddSingleton<IPunditOpinionExtractor, StubPunditOpinionExtractor>();
        }

        // Reaction GIF provider: AI picks the reaction phrase, Giphy (or legacy Tenor) supplies a live GIF.
        // Falls back to the bundled local sticker repository when no API key is configured.
        var reactionOpts = configuration.GetSection(ReactionGifOptions.SectionName).Get<ReactionGifOptions>()
            ?? new ReactionGifOptions();

        services.AddSingleton<IReactionGifLedger, ReactionGifLedger>();
        if (reactionOpts.IsGiphyEnabled)
        {
            services.AddHttpClient<IReactionGifProvider, GiphyGifProvider>();
        }
        else if (reactionOpts.IsTenorEnabled)
        {
            services.AddHttpClient<IReactionGifProvider, TenorGifProvider>();
        }
        else
        {
            services.TryAddSingleton<IReactionGifProvider, NullReactionGifProvider>();
        }

        services.AddScoped<ReactionMediaResolver>();
        services.AddScoped<FeedReactionMediaService>();
        services.AddScoped<MatchResolutionService>();
        services.AddScoped<FeedRelevanceScorer>();

        services.AddScoped<PunditMediaItemService>();
        services.AddScoped<PunditReviewFlagger>();
        services.AddScoped<PunditOpinionPersistenceService>();
        services.AddScoped<PredictionAggregateService>();
        services.AddScoped<SyncRunTracker>();
        services.AddScoped<ScoreSyncJob>();
        services.AddScoped<StandingsSyncJob>();
        services.AddScoped<MatchDetailsSyncJob>();
        services.AddScoped<NewsIngestJob>();
        services.AddScoped<MediaIngestJob>();
        services.AddScoped<AiReactionJob>();
        services.AddScoped<FeedBanterEnrichmentJob>();
        services.AddScoped<YouTubeSearchSyncJob>();
        services.AddScoped<RssOpinionSyncJob>();
        services.AddScoped<RssFeedResolveJob>();
        services.AddScoped<ContentEnrichmentJob>();
        services.AddScoped<PunditExtractionJob>();
        services.AddScoped<PredictionAggregateJob>();
        services.AddScoped<IJobRegistryService, JobRegistryService>();
        services.AddScoped<StubMaintenanceJobs>();

        return services;
    }
}
