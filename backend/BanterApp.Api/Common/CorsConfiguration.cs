namespace BanterApp.Api.Common;

/// <summary>
/// Resolves the CORS allow-list for the API.
/// Production (e.g. Render) sets the <c>ALLOWED_ORIGINS</c> environment variable as a
/// comma-separated list (read here via <see cref="IConfiguration"/>, which includes the
/// environment-variable source). Falls back to the <c>Cors:AllowedOrigins</c> config
/// section, then to localhost in Development only. Production never defaults to localhost.
/// </summary>
public static class CorsConfiguration
{
    public const string DevelopmentOrigin = "http://localhost:3000";

    public static string[] ResolveAllowedOrigins(IConfiguration configuration, bool isDevelopment)
    {
        var fromEnv = configuration["ALLOWED_ORIGINS"];
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var fromConfig = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (fromConfig is { Length: > 0 })
        {
            return fromConfig;
        }

        return isDevelopment ? [DevelopmentOrigin] : [];
    }
}
