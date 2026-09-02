using System.Net;
using System.Text;
using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests;

public class GiphyGifProviderTests
{
    [Fact]
    public void ExtractGifUrls_ParsesGiphySearchResponse()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "data": [
                {
                  "id": "abc123",
                  "images": {
                    "original": { "url": "https://media1.giphy.com/media/abc123/giphy.gif" },
                    "fixed_height": { "url": "https://media2.giphy.com/media/abc123/200.gif" }
                  }
                },
                {
                  "id": "def456",
                  "images": {
                    "downsized": { "url": "https://media3.giphy.com/media/def456/giphy-downsized.gif" }
                  }
                }
              ]
            }
            """);

        var hits = GiphyResponseParser.ExtractHits(doc.RootElement);

        Assert.Equal(2, hits.Count);
        Assert.Equal("abc123", hits[0].Id);
        Assert.Equal("https://media1.giphy.com/media/abc123/giphy.gif", hits[0].Url);
        Assert.Equal("def456", hits[1].Id);
    }

    [Fact]
    public void ExtractHits_ParsesRandomEndpointObject()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "data": {
                "id": "rand1",
                "images": {
                  "original": { "url": "https://media4.giphy.com/media/rand1/giphy.gif" }
                }
              }
            }
            """);

        var hits = GiphyResponseParser.ExtractHits(doc.RootElement);

        Assert.Equal("rand1", hits[0].Id);
        Assert.Equal("https://media4.giphy.com/media/rand1/giphy.gif", hits[0].Url);
    }

    [Fact]
    public void FromUrl_UsesMediaIdNotCdnHost()
    {
        Assert.Equal(
            "abc123xyz",
            GiphyGifSelector.FromUrl("https://media1.giphy.com/media/abc123xyz/giphy.gif"));
        Assert.Equal(
            "abc123xyz",
            GiphyGifSelector.FromUrl("https://media4.giphy.com/media/abc123xyz/200.gif"));
        Assert.Equal(
            "abc123xyz",
            GiphyGifSelector.FromUrl("https://i.giphy.com/abc123xyz.gif"));
    }

    [Fact]
    public void ReactionGifOptions_DefaultProviderIsGiphy()
    {
        var options = new ReactionGifOptions { ApiKey = "test-key" };

        Assert.True(options.IsGiphyEnabled);
        Assert.False(options.IsTenorEnabled);
    }

    [Fact]
    public void TruncateQuery_HonorsGiphyFiftyCharLimit()
    {
        var longQuery = new string('x', 80);
        Assert.Equal(50, GiphyGifSelector.TruncateQuery(longQuery).Length);
    }

    [Fact]
    public async Task FindGifUrlAsync_PrefersRandomEndpoint()
    {
        var handler = new StubHandler(uri =>
        {
            Assert.Contains("/gifs/random", uri);
            Assert.DoesNotContain("/gifs/search", uri);
            return RandomResponse("randA", "https://media1.giphy.com/media/randA/giphy.gif");
        });
        var provider = CreateProvider(handler);

        var url = await provider.FindGifUrlAsync("soccer celebration", seed: 2);

        Assert.Equal("https://media1.giphy.com/media/randA/giphy.gif", url);
        Assert.Single(handler.Requests);
        Assert.Contains("tag=soccer", handler.Requests[0]);
    }

    [Fact]
    public async Task FindGifUrlAsync_SameSeedReturnsTheSameGifWithoutRefetch()
    {
        var handler = new StubHandler(_ =>
            RandomResponse("stable", "https://media1.giphy.com/media/stable/giphy.gif"));
        var provider = CreateProvider(handler);

        var first = await provider.FindGifUrlAsync("hype", seed: 11);
        var second = await provider.FindGifUrlAsync("hype", seed: 11);

        Assert.Equal(first, second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task FindGifUrlAsync_RetriesRandomWhenGifAlreadyUsedThisWindow()
    {
        var randomCalls = 0;
        var handler = new StubHandler(uri =>
        {
            if (uri.Contains("/gifs/random"))
            {
                randomCalls++;
                return randomCalls == 1
                    ? RandomResponse("used1", "https://media1.giphy.com/media/used1/giphy.gif")
                    : RandomResponse("fresh2", "https://media1.giphy.com/media/fresh2/giphy.gif");
            }

            return SearchResponse();
        });
        var ledger = new InMemoryReactionGifLedger();
        await ledger.TryClaimAsync(seed: -1, "used1", "https://media1.giphy.com/media/used1/giphy.gif");
        var provider = CreateProvider(handler, ledger);

        var url = await provider.FindGifUrlAsync("goal", seed: 4);

        Assert.Equal("https://media1.giphy.com/media/fresh2/giphy.gif", url);
        Assert.Equal(2, randomCalls);
    }

    [Fact]
    public async Task FindGifUrlAsync_DifferentSeedsDoNotShareAGif()
    {
        var n = 0;
        var handler = new StubHandler(_ =>
        {
            n++;
            return RandomResponse($"gif{n}", $"https://media1.giphy.com/media/gif{n}/giphy.gif");
        });
        var provider = CreateProvider(handler);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var seed = 0; seed < 8; seed++)
        {
            var url = await provider.FindGifUrlAsync("football reaction", seed);
            Assert.False(string.IsNullOrWhiteSpace(url));
            Assert.True(seen.Add(url!), $"Repeated GIF {url} for seed {seed}");
        }
    }

    [Fact]
    public async Task FindGifUrlAsync_FallsBackToShuffledSearchWhenRandomIsExhausted()
    {
        var handler = new StubHandler(uri =>
            uri.Contains("/gifs/random")
                ? RandomResponse("used1", "https://media1.giphy.com/media/used1/giphy.gif")
                : SearchResponse(("freshS", "https://media1.giphy.com/media/freshS/giphy.gif")));
        var ledger = new InMemoryReactionGifLedger();
        await ledger.TryClaimAsync(-1, "used1", "https://media1.giphy.com/media/used1/giphy.gif");
        var provider = CreateProvider(handler, ledger);

        var url = await provider.FindGifUrlAsync("football reaction", seed: 7);

        Assert.Equal("https://media1.giphy.com/media/freshS/giphy.gif", url);
        Assert.Contains(handler.Requests, r => r.Contains("/gifs/search"));
    }

    [Fact]
    public async Task Ledger_HydratesUsedIdsFromFeedItemsInTheCurrentWindow()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(databaseName));
        await using var sp = services.BuildServiceProvider();

        var window = GameweekGifWindow.Current();
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.NewsFeedItems.Add(new NewsFeedItem
            {
                Id = "feed-1",
                Source = "test",
                Title = "title",
                Url = "https://example.test/1",
                ImageUrl = "https://media1.giphy.com/media/alreadyShown/giphy.gif",
                PublishedAt = window.StartUtc.AddHours(2),
            });
            await db.SaveChangesAsync();
        }

        var ledger = new ReactionGifLedger(
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReactionGifLedger>.Instance);

        var claimed = await ledger.TryClaimAsync(
            seed: 9,
            gifId: "alreadyShown",
            url: "https://media1.giphy.com/media/alreadyShown/giphy.gif");

        Assert.False(claimed);
    }

    private static GiphyGifProvider CreateProvider(StubHandler handler, IReactionGifLedger? ledger = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.giphy.com/") };
        var options = Options.Create(new ReactionGifOptions
        {
            Provider = "giphy",
            ApiKey = "test-key",
            SearchLimit = 25,
        });
        return new GiphyGifProvider(
            http,
            options,
            ledger ?? new InMemoryReactionGifLedger(),
            NullLogger<GiphyGifProvider>.Instance);
    }

    private static HttpResponseMessage SearchResponse(params (string Id, string Url)[] gifs)
    {
        var items = string.Join(",", gifs.Select(g =>
            "{\"id\":\"" + g.Id + "\",\"images\":{\"original\":{\"url\":\"" + g.Url + "\"}}}"));
        return JsonResponse("{\"data\":[" + items + "]}");
    }

    private static HttpResponseMessage RandomResponse(string id, string url) =>
        JsonResponse("{\"data\":{\"id\":\"" + id + "\",\"images\":{\"original\":{\"url\":\"" + url + "\"}}}}");

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHandler(Func<string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            Requests.Add(uri);
            return Task.FromResult(respond(uri));
        }
    }
}
