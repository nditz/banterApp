using BanterApp.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace BanterApp.Api.Tests;

public class ProductionStartupValidatorTests
{
    [Fact]
    public async Task Validate_SkipsOutsideProduction()
    {
        var validator = Create(Environments.Development, []);
        await validator.ValidateAsync();
    }

    [Fact]
    public async Task Validate_Production_RequiresJwtSecret()
    {
        var validator = Create(Environments.Production, ValidProduction(remove: "Supabase:JwtSecret"));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAsync());
        Assert.Contains("Supabase:JwtSecret", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_Production_RejectsLocalhostCors()
    {
        var values = ValidProduction();
        values["Cors:AllowedOrigins:0"] = "http://localhost:3000";
        var validator = Create(Environments.Production, values);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAsync());
        Assert.Contains("localhost", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validate_Production_AcceptsCompleteConfig()
    {
        var validator = Create(Environments.Production, ValidProduction());
        await validator.ValidateAsync();
    }

    private static Dictionary<string, string?> ValidProduction(string? remove = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres",
            ["Supabase:JwtSecret"] = "jwt-secret",
            ["Security:SessionSecret"] = "production-session-secret-32chars!",
            ["Security:TurnstileSecretKey"] = "turnstile",
            ["YouTube:ApiKey"] = "youtube",
            ["Legal:DisclaimerText"] = "Disclaimer",
            ["Legal:TermsUrl"] = "https://balltakes.com/terms",
            ["Legal:PrivacyPolicyUrl"] = "https://balltakes.com/privacy",
            ["Admin:AllowedEmails:0"] = "admin@balltakes.com",
        };

        if (remove is not null)
        {
            values.Remove(remove);
        }

        return values;
    }

    private static ProductionStartupValidator Create(string environmentName, Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection().BuildServiceProvider();
        return new ProductionStartupValidator(config, new StubWebHostEnvironment(environmentName), services);
    }

    private sealed class StubWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "BanterApp.Api.Tests";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
