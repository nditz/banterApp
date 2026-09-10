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
            Assert.Contains("Premier League", q, StringComparison.OrdinalIgnoreCase));
    }
}
