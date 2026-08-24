using BanterApp.Api.Integrations.SportsData;
using Xunit;

namespace BanterApp.Api.Tests;

public class ClubBadgesTests
{
    [Theory]
    [InlineData("ARS", "Arsenal", "42")]
    [InlineData("MCI", "Manchester City", "50")]
    [InlineData("NEW", "Newcastle", "34")]
    [InlineData("SUN", "Sunderland", "746")]
    [InlineData("COV", "Coventry City", "71")]
    [InlineData("HUL", "Hull City", "64")]
    [InlineData("IPS", "Ipswich Town", "57")]
    public void UrlFor_ResolvesPremierLeagueClub(string code, string name, string teamId)
    {
        var url = ClubBadges.UrlFor(code, name);
        Assert.Equal($"https://media.api-sports.io/football/teams/{teamId}.png", url);
    }

    [Fact]
    public void Coalesce_KeepsProviderLogoWhenPresent()
    {
        var provider = "https://crests.football-data.org/57.png";
        Assert.Equal(provider, ClubBadges.Coalesce(provider, "ARS", "Arsenal"));
    }

    [Fact]
    public void Coalesce_FillsMissingLogoFromClubMap()
    {
        Assert.Equal(
            "https://media.api-sports.io/football/teams/42.png",
            ClubBadges.Coalesce(null, "ARS", "Arsenal"));
    }
}
