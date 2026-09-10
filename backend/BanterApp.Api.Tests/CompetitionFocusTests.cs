using BanterApp.Api.Data;
using Xunit;

namespace BanterApp.Api.Tests;

public class CompetitionFocusTests
{
    [Fact]
    public void DisplayName_IsPremierLeagueSeason()
    {
        Assert.Equal("Premier League", CompetitionFocus.Name);
        Assert.Contains("2026/27", CompetitionFocus.DisplayName, StringComparison.Ordinal);
        Assert.Contains("Premier League", CompetitionFocus.PromptDirective, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ignore World Cup", CompetitionFocus.PromptDirective, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("World Cup hydration breaks", true)]
    [InlineData("https://www.fifa.com/tournaments/mens/worldcup/qatar2022", true)]
    [InlineData("https://www.theguardian.com/football/world-cup/2026", true)]
    [InlineData("Arsenal vs Chelsea Premier League preview", false)]
    [InlineData("FIFA 26 career mode", false)]
    [InlineData("", false)]
    public void LooksLikeOffFocus_DetectsWorldCupNotGenericFifa(string value, bool expected)
    {
        Assert.Equal(expected, CompetitionFocus.LooksLikeOffFocus(value));
    }

    [Fact]
    public void ApplyToSystemPrompt_PrependsDirectiveOnce()
    {
        var first = CompetitionFocus.ApplyToSystemPrompt("Write banter.");
        var second = CompetitionFocus.ApplyToSystemPrompt(first);

        Assert.StartsWith(CompetitionFocus.PromptDirective, first, StringComparison.Ordinal);
        Assert.Equal(first, second);
    }

    [Fact]
    public void YouTubeSearchQueries_ArePremierLeagueSpecific()
    {
        Assert.NotEmpty(CompetitionFocus.YouTubeSearchQueries);
        Assert.All(CompetitionFocus.YouTubeSearchQueries, q =>
        {
            Assert.Contains("Premier League", q, StringComparison.OrdinalIgnoreCase);
            Assert.False(CompetitionFocus.LooksLikeOffFocus(q));
        });
    }
}
