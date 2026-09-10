using BanterApp.Api.Data;
using BanterApp.Api.Features.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanterApp.Api.Tests.Metrics;

public sealed class ProductMetricServiceTests
{
    [Fact]
    public async Task Records_an_allowlisted_key()
    {
        await using var db = CreateDb();
        var service = new ProductMetricService(db, NullLogger<ProductMetricService>.Instance);

        await service.RecordAsync(ProductMetrics.PredictionMade);

        var stored = Assert.Single(db.AppMetrics);
        Assert.Equal(ProductMetrics.PredictionMade, stored.MetricKey);
        Assert.Equal(1, stored.MetricValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("user_email")]
    [InlineData("Prediction_Made")]
    public async Task Drops_anything_outside_the_allowlist(string key)
    {
        await using var db = CreateDb();
        var service = new ProductMetricService(db, NullLogger<ProductMetricService>.Instance);

        await service.RecordAsync(key);

        Assert.Empty(db.AppMetrics);
    }

    [Fact]
    public async Task Summary_reports_zero_for_keys_with_no_events()
    {
        await using var db = CreateDb();
        var service = new ProductMetricService(db, NullLogger<ProductMetricService>.Instance);
        await service.RecordAsync(ProductMetrics.StudioOpened);
        await service.RecordAsync(ProductMetrics.StudioOpened);

        var summary = await service.SummarizeAsync(DateTimeOffset.UtcNow.AddDays(-1));

        Assert.Equal(2, summary[ProductMetrics.StudioOpened]);
        Assert.Equal(0, summary[ProductMetrics.ContentExported]);
        Assert.Equal(ProductMetrics.Allowed.Count, summary.Count);
    }

    [Fact]
    public async Task Summary_ignores_events_before_the_window()
    {
        await using var db = CreateDb();
        var service = new ProductMetricService(db, NullLogger<ProductMetricService>.Instance);
        await service.RecordAsync(ProductMetrics.LeagueJoined);
        db.AppMetrics.Single().RecordedAt = DateTimeOffset.UtcNow.AddDays(-40);
        await db.SaveChangesAsync();

        var summary = await service.SummarizeAsync(DateTimeOffset.UtcNow.AddDays(-7));

        Assert.Equal(0, summary[ProductMetrics.LeagueJoined]);
    }

    [Fact]
    public async Task HasAny_distinguishes_never_wired_from_zero()
    {
        await using var db = CreateDb();
        var service = new ProductMetricService(db, NullLogger<ProductMetricService>.Instance);

        Assert.False(await service.HasAnyAsync());

        await service.RecordAsync(ProductMetrics.ReceiptViewed);

        Assert.True(await service.HasAnyAsync());
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
