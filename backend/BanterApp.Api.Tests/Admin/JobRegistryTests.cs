using BanterApp.Api.Integrations.Jobs;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class JobRegistryTests
{
    [Theory]
    [InlineData("rss.sync", "rss-opinion-sync")]
    [InlineData("youtube.search.sync", "youtube-opinion-sync")]
    [InlineData("openai.opinion.extract", "pundit-extraction")]
    [InlineData("predictions.aggregate.refresh", "prediction-aggregate-refresh")]
    [InlineData("analytics.retention.cleanup", "analytics-retention-cleanup")]
    public void FindByKey_MapsSpecKeysToHangfireIds(string key, string hangfireId)
    {
        var job = JobRegistry.FindByKey(key);

        Assert.NotNull(job);
        Assert.Equal(hangfireId, job!.HangfireJobId);
    }

    [Fact]
    public void FindByKey_IsCaseInsensitive()
    {
        var job = JobRegistry.FindByKey("RSS.SYNC");

        Assert.NotNull(job);
        Assert.Equal("rss-opinion-sync", job!.HangfireJobId);
    }

    [Fact]
    public void FindByKey_UnknownKey_ReturnsNull()
    {
        Assert.Null(JobRegistry.FindByKey("not-a-real-job"));
    }

    [Fact]
    public void All_ContainsStubMaintenanceJobs()
    {
        var keys = JobRegistry.All.Select(j => j.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("failed-items.retry", keys);
        Assert.Contains("stale-content.cleanup", keys);
        Assert.True(JobRegistry.All.First(j => j.Key == "failed-items.retry").IsStub);
    }

    [Fact]
    public void FindByHangfireId_ReturnsMatchingDefinition()
    {
        var job = JobRegistry.FindByHangfireId("pundit-extraction");

        Assert.NotNull(job);
        Assert.Equal("openai.opinion.extract", job!.Key);
    }
}
