using BanterApp.Api.Integrations.Rss;
using Xunit;

namespace BanterApp.Api.Tests.Rss;

public class RssSourcePolicyTests
{
    [Theory]
    [InlineData("https://feeds.bbci.co.uk/sport/football/rss.xml")]
    [InlineData("https://podcasts.files.bbci.co.uk/p02nrsln.rss")]
    [InlineData("https://www.bbc.co.uk/sport/football")]
    [InlineData("https://bbc.com/sport")]
    public void IsDisallowed_BbcHosts(string url)
    {
        Assert.True(RssSourcePolicy.IsDisallowed(url));
    }

    [Theory]
    [InlineData("https://www.theguardian.com/football/rss")]
    [InlineData("https://feeds.megaphone.fm/GLT8847082992")]
    [InlineData("")]
    [InlineData("not-a-url")]
    public void IsDisallowed_AllowsOtherHosts(string url)
    {
        Assert.False(RssSourcePolicy.IsDisallowed(url));
    }
}
