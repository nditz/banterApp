using BanterApp.Api.Integrations.Rss;
using Xunit;

namespace BanterApp.Api.Tests.Rss;

public class OpenAiRssUrlDiscoveryTests
{
    [Theory]
    [InlineData("""{"rss_url":"https://feeds.megaphone.fm/GLT8847082992"}""", "https://feeds.megaphone.fm/GLT8847082992")]
    [InlineData("""{"rss_url":null}""", null)]
    [InlineData("", null)]
    [InlineData("```json\n{\"rss_url\":\"https://www.theguardian.com/football/rss\"}\n```", "https://www.theguardian.com/football/rss")]
    public void ParseRssUrl_ReadsModelOutput(string json, string? expected)
    {
        Assert.Equal(expected, OpenAiRssUrlDiscovery.ParseRssUrl(json));
    }
}
