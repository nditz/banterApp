using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Integrations.FootballReference;

public sealed class FootballReferenceDataProviderFactory(
    IServiceProvider serviceProvider,
    IOptions<FootballReferenceDataOptions> options,
    ILogger<FootballReferenceDataProviderFactory> logger)
{
    public IFootballReferenceDataProvider Resolve()
    {
        var provider = options.Value.Provider?.Trim().ToLowerInvariant() ?? "api_sports";

        IFootballReferenceDataProvider? resolved = provider switch
        {
            "api_sports" or "apifootball" or "api-football" =>
                serviceProvider.GetService<ApiSportsReferenceProvider>(),
            "sportmonks" =>
                serviceProvider.GetService<SportmonksReferenceProvider>(),
            "googleapis" or "google" =>
                serviceProvider.GetService<GoogleReferenceProviderStub>(),
            _ => null
        };

        if (resolved is null)
        {
            logger.LogWarning(
                "Unknown FootballReferenceData provider '{Provider}'; using no-op provider.",
                provider);
            return serviceProvider.GetRequiredService<NoOpReferenceProvider>();
        }

        if (!resolved.IsConfigured)
        {
            logger.LogWarning(
                "Football reference provider '{Provider}' is not configured (missing API key); using no-op.",
                resolved.ProviderName);
            return serviceProvider.GetRequiredService<NoOpReferenceProvider>();
        }

        return resolved;
    }
}
