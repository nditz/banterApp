using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BanterApp.Api.Data;

/// <summary>
/// Allows <c>dotnet ef</c> to load the same configuration as the running API
/// (<c>appsettings.json</c> + <c>appsettings.Development.json</c> + environment variables).
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = DatabaseConnection.Resolve(configuration)
            ?? throw new InvalidOperationException(
                "No database connection found. Set ConnectionStrings:DefaultConnection in " +
                "appsettings.Development.json (local) or ConnectionStrings__DefaultConnection " +
                "(GitHub/hosting secret at deploy time).");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
