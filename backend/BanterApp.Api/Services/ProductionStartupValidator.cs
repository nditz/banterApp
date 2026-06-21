using BanterApp.Api.Data;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Integrations.Ai;
using BanterApp.Api.Integrations.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Services;

public sealed class ProductionStartupValidator(
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IServiceProvider services)
{
    public const string DevSessionSecret = "banter-dev-session-secret-change-in-production";

    public async Task ValidateAsync(CancellationToken ct = default)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var errors = new List<string>();

        var connectionString = DatabaseConnection.Resolve(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("Production requires ConnectionStrings:DefaultConnection or Database:DirectUrl.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Supabase:JwtSecret"]))
        {
            errors.Add("Production requires Supabase:JwtSecret.");
        }

        var sessionSecret = configuration["Security:SessionSecret"];
        if (string.IsNullOrWhiteSpace(sessionSecret) || sessionSecret == DevSessionSecret)
        {
            errors.Add("Production requires Security:SessionSecret (not the dev default).");
        }

        if (string.IsNullOrWhiteSpace(configuration["Security:TurnstileSecretKey"]))
        {
            errors.Add("Production requires Security:TurnstileSecretKey.");
        }

        var aiProvider = configuration["Ai:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        if (aiProvider is "openai" or "chatgpt" && string.IsNullOrWhiteSpace(configuration["Ai:ApiKey"]))
        {
            errors.Add("Production requires Ai:ApiKey when Ai:Provider is openai.");
        }

        if (string.IsNullOrWhiteSpace(configuration["YouTube:ApiKey"]))
        {
            errors.Add("Production requires YouTube:ApiKey.");
        }

        var legal = configuration.GetSection(LegalOptions.SectionName).Get<LegalOptions>() ?? new LegalOptions();
        if (string.IsNullOrWhiteSpace(legal.DisclaimerText))
        {
            errors.Add("Production requires Legal:DisclaimerText.");
        }

        if (string.IsNullOrWhiteSpace(legal.TermsUrl))
        {
            errors.Add("Production requires Legal:TermsUrl.");
        }

        if (string.IsNullOrWhiteSpace(legal.PrivacyPolicyUrl))
        {
            errors.Add("Production requires Legal:PrivacyPolicyUrl.");
        }

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Any(o => o.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Production Cors:AllowedOrigins must not include localhost.");
        }

        var adminOptions = configuration.GetSection(AdminOptions.SectionName).Get<AdminOptions>() ?? new AdminOptions();
        var hasAdminConfig = adminOptions.AllowedEmails.Count > 0 || adminOptions.AllowedUserIds.Count > 0;

        if (!hasAdminConfig)
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.IsRelational())
            {
                var hasDbAdmin = await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct);
                if (!hasDbAdmin)
                {
                    errors.Add("Production requires Admin:AllowedEmails or an IsPlatformAdmin user.");
                }
            }
            else
            {
                errors.Add("Production requires Admin:AllowedEmails or an IsPlatformAdmin user.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Production startup validation failed:\n- " + string.Join("\n- ", errors));
        }
    }
}
