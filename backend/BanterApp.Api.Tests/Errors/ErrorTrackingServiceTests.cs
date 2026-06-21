using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ErrorTrackingServiceTests
{
    private static IErrorTrackingService CreateTracking(string dbName, out AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddSingleton<IHostEnvironment>(new TestWebHostEnvironment());
        services.AddScoped<IErrorTrackingService, ErrorTrackingService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return scope.ServiceProvider.GetRequiredService<IErrorTrackingService>();
    }

    [Fact]
    public async Task TrackAsync_PersistsSanitizedError()
    {
        var dbName = Guid.NewGuid().ToString();
        var tracking = CreateTracking(dbName, out var db);

        await tracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "backend",
            ErrorCode = ErrorCodes.InternalServerError,
            MessageSafe = "Something failed",
            MessageInternal = "api_key=secret-value",
            Provider = "app"
        });

        var row = await db.OperationalErrors.SingleAsync();
        Assert.Equal("Something failed", row.MessageSafe);
        Assert.DoesNotContain("secret-value", row.MessageInternal ?? string.Empty);
    }

    [Fact]
    public async Task TrackAsync_DeduplicatesByFingerprint()
    {
        var dbName = Guid.NewGuid().ToString();
        var tracking = CreateTracking(dbName, out var db);

        for (var i = 0; i < 2; i++)
        {
            await tracking.TrackAsync(new ErrorTrackRequest
            {
                Source = "backend",
                ErrorCode = ErrorCodes.OpenAiApiError,
                MessageSafe = "OpenAI request failed",
                Route = "/api/ai/generate",
                Provider = "openai"
            });
        }

        var rows = await db.OperationalErrors.ToListAsync();
        Assert.Single(rows);
        Assert.Equal(2, rows[0].OccurrenceCount);
    }

    [Fact]
    public async Task TrackAsync_ReopensResolvedError()
    {
        var dbName = Guid.NewGuid().ToString();
        var tracking = CreateTracking(dbName, out var db);

        await tracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "job",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = "Job failed",
            JobKey = "rss.sync"
        });

        var existing = await db.OperationalErrors.SingleAsync();
        existing.Status = "resolved";
        existing.ResolvedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        await tracking.TrackAsync(new ErrorTrackRequest
        {
            Source = "job",
            ErrorCode = ErrorCodes.JobFailed,
            MessageSafe = "Job failed",
            JobKey = "rss.sync"
        });

        db.ChangeTracker.Clear();
        var row = await db.OperationalErrors.SingleAsync();
        Assert.Equal("open", row.Status);
        Assert.Equal(2, row.OccurrenceCount);
    }

    private sealed class TestWebHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "BanterApp.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
