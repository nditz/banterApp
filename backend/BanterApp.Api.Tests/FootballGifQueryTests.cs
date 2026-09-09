using BanterApp.Api.Integrations.Media;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballGifQueryTests
{
    [Theory]
    [InlineData("premier league", true)]
    [InlineData("Haaland celebration", true)]
    [InlineData("goal of the season", true)]
    [InlineData("happy birthday", false)]
    [InlineData("cat dancing", false)]
    [InlineData("", false)]
    public void LooksFootball_DetectsFootballPhrases(string phrase, bool expected)
    {
        Assert.Equal(expected, FootballGifQuery.LooksFootball(phrase));
    }
}
