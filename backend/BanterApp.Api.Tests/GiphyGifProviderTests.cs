using System.Text.Json;
using BanterApp.Api.Integrations.Media;
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
                  "images": {
                    "original": { "url": "https://media1.giphy.com/media/abc123/giphy.gif" },
                    "fixed_height": { "url": "https://media2.giphy.com/media/abc123/200.gif" }
                  }
                },
                {
                  "images": {
                    "downsized": { "url": "https://media3.giphy.com/media/def456/giphy-downsized.gif" }
                  }
                }
              ]
            }
            """);

        var urls = GiphyResponseParser.ExtractGifUrls(doc.RootElement);

        Assert.Equal(2, urls.Length);
        Assert.Equal("https://media1.giphy.com/media/abc123/giphy.gif", urls[0]);
        Assert.Equal("https://media3.giphy.com/media/def456/giphy-downsized.gif", urls[1]);
    }

    [Fact]
    public void ReactionGifOptions_DefaultProviderIsGiphy()
    {
        var options = new ReactionGifOptions { ApiKey = "test-key" };

        Assert.True(options.IsGiphyEnabled);
        Assert.False(options.IsTenorEnabled);
    }
}
