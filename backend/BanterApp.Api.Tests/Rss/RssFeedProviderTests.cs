using System.Net;
using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BanterApp.Api.Tests.Rss;

public class RssFeedProviderTests
{
    [Fact]
    public async Task FetchFeedAsync_SkipsBbcWithoutHttp()
    {
        var http = new CountingSafeHttpClient();
        var provider = new RssFeedProvider(
            http,
            NullLogger<RssFeedProvider>.Instance,
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

        var result = await provider.FetchFeedAsync(
            "https://feeds.bbci.co.uk/sport/football/rss.xml",
            maxItems: 10);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Items);
        Assert.Equal(0, http.Calls);
    }

    [Fact]
    public void TruncateToCompleteItems_KeepsLastFullRssItem()
    {
        const string xml = """
            <rss><channel>
            <item><title>One</title><link>https://example.com/1</link><guid>1</guid></item>
            <item><title>Two</title><link>https://example.com/2</link><guid>2</guid></item>
            <item><title>Trun
            """;

        var truncated = RssFeedProvider.TruncateToCompleteItems(xml);
        Assert.NotNull(truncated);
        Assert.Contains("<title>Two</title>", truncated);
        Assert.DoesNotContain("<title>Trun", truncated);

        var items = RssFeedProvider.TryParsePartialFeed(xml, "https://example.com/feed", 10, null, false);
        Assert.Equal(2, items.Count);
        Assert.Equal("One", items[0].Title);
        Assert.Equal("Two", items[1].Title);
    }

    [Fact]
    public async Task FetchFeedAsync_OversizedFeed_ParsesCompleteItems()
    {
        var xml = """
            <rss><channel>
            <item><title>Kept</title><link>https://example.com/kept</link><guid>kept</guid></item>
            <item><title>Partial
            """;
        var http = new ScriptedSafeHttpClient(new SafeHttpFetchResult(
            new SafeHttpResponse(xml, "application/rss+xml", HttpStatusCode.OK, "https://example.com/feed"),
            SafeHttpFailureKind.Oversized,
            "oversized bytes_read=6000000 limit=5242880 content_length=unknown"));
        var provider = new RssFeedProvider(
            http,
            NullLogger<RssFeedProvider>.Instance,
            new ServiceCollection().AddLogging().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

        var result = await provider.FetchFeedAsync("https://example.com/feed", maxItems: 10);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        var item = Assert.Single(result.Items);
        Assert.Equal("Kept", item.Title);
    }

    [Fact]
    public async Task FetchFeedAsync_Ssrf_ReturnsFailureWithUrl()
    {
        var http = new ScriptedSafeHttpClient(SafeHttpFetchResult.Fail(SafeHttpFailureKind.Ssrf, "host_not_allowed"));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IErrorTrackingService, RecordingErrorTracking>();
        var provider = new RssFeedProvider(
            http,
            NullLogger<RssFeedProvider>.Instance,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

        var result = await provider.FetchFeedAsync("https://blocked.example/rss", maxItems: 5);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Failure);
        Assert.Equal("host_not_allowed", result.Failure.Reason);
        Assert.Contains("https://blocked.example/rss", result.Failure.SafeMessage);
        Assert.Contains("host_not_allowed", result.Failure.SafeMessage);
    }
}

file sealed class CountingSafeHttpClient : ISafeHttpClient
{
    public int Calls { get; private set; }

    public Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default)
    {
        Calls++;
        return Task.FromResult<SafeHttpResponse?>(null);
    }
}

file sealed class ScriptedSafeHttpClient(SafeHttpFetchResult result) : ISafeHttpClient
{
    public Task<SafeHttpResponse?> GetStringAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(result.Response);

    public Task<SafeHttpFetchResult> FetchAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(result);

    public Task<SafeHttpFetchResult> FetchAsync(string url, int? maxResponseBytes, CancellationToken ct = default) =>
        Task.FromResult(result);
}

file sealed class RecordingErrorTracking : IErrorTrackingService
{
    public List<ErrorTrackRequest> Requests { get; } = [];

    public Task<Guid> TrackAsync(ErrorTrackRequest request, CancellationToken ct = default)
    {
        Requests.Add(request);
        return Task.FromResult(Guid.NewGuid());
    }

    public Task<Guid> TrackExceptionAsync(ErrorTrackRequest request, Exception exception, CancellationToken ct = default) =>
        TrackAsync(request, ct);
}
