using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.News;
using BanterApp.Api.Integrations.SportsData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BanterApp.Api.Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBanterIntegrations(this IServiceCollection services)
    {
        var sportsProvider = Environment.GetEnvironmentVariable("SPORTS_API_PROVIDER")?.Trim().ToLowerInvariant()
            ?? "mock";

        var sportsApiKey = Environment.GetEnvironmentVariable("SPORTS_API_KEY");

        services.Configure<SportsDataOptions>(options =>
        {
            options.Provider = sportsProvider;
            options.ApiKey = string.IsNullOrWhiteSpace(sportsApiKey) ? null : sportsApiKey;
        });

        switch (sportsProvider)
        {
            case "apifootball":
                services.AddHttpClient<ISportsDataProvider, ApiFootballProvider>();
                break;

            case "mock":
            default:
                if (sportsProvider != "mock")
                {
                    Console.Error.WriteLine(
                        $"Unknown SPORTS_API_PROVIDER '{sportsProvider}'; falling back to mock.");
                }

                services.TryAddSingleton<ISportsDataProvider, MockSportsDataProvider>();
                break;
        }

        var newsApiKey = Environment.GetEnvironmentVariable("NEWS_API_KEY");
        if (!string.IsNullOrWhiteSpace(newsApiKey))
        {
            services.AddHttpClient<INewsProvider, NewsApiProvider>();
        }
        else
        {
            services.TryAddSingleton<INewsProvider, MockNewsProvider>();
        }

        services.TryAddSingleton<IContentGenerator, StubContentGenerator>();
        services.AddHostedService<SportsDataSyncService>();

        return services;
    }
}
