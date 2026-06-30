using BanterApp.Api.Common;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BanterApp.Api.Tests;

public class CorsConfigurationTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void ResolveAllowedOrigins_ParsesCommaSeparatedAllowedOrigins()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ALLOWED_ORIGINS"] = "https://balltakes.com,https://www.balltakes.com",
        });

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.Equal(["https://balltakes.com", "https://www.balltakes.com"], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_TrimsWhitespaceAndSkipsEmpties()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ALLOWED_ORIGINS"] = " https://balltakes.com , , https://www.balltakes.com ",
        });

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.Equal(["https://balltakes.com", "https://www.balltakes.com"], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_FallsBackToCorsSection()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://balltakes.com",
            ["Cors:AllowedOrigins:1"] = "https://www.balltakes.com",
        });

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.Equal(["https://balltakes.com", "https://www.balltakes.com"], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_AllowedOriginsEnvTakesPrecedenceOverCorsSection()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ALLOWED_ORIGINS"] = "https://balltakes.com",
            ["Cors:AllowedOrigins:0"] = "https://ignored.example.com",
        });

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.Equal(["https://balltakes.com"], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_Development_DefaultsToLocalhost()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: true);

        Assert.Equal([CorsConfiguration.DevelopmentOrigin], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_Production_DefaultsToEmpty()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.Empty(origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_Production_NeverAllowsLocalhost()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ALLOWED_ORIGINS"] = "https://balltakes.com,https://www.balltakes.com",
        });

        var origins = CorsConfiguration.ResolveAllowedOrigins(config, isDevelopment: false);

        Assert.DoesNotContain(origins, o => o.Contains("localhost", StringComparison.OrdinalIgnoreCase));
    }
}
