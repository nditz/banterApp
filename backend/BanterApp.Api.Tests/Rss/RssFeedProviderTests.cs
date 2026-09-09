using BanterApp.Api.Integrations.Media;
using BanterApp.Api.Integrations.Media.Dtos;
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

        var items = await provider.FetchFeedAsync(
            "https://feeds.bbci.co.uk/sport/football/rss.xml",
            maxItems: 10);

        Assert.Empty(items);
        Assert.Equal(0, http.Calls);
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
