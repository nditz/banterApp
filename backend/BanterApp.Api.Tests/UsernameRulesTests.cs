using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests;

public class UsernameRulesTests
{
    [Theory]
    [InlineData("Shadowfox", true)]
    [InlineData("Player42", true)]
    [InlineData("ab", false)]
    [InlineData("bad-name", false)]
    [InlineData("spaces here", false)]
    [InlineData("ToolongUsernameHere123456", false)]
    public void IsValidFormat_ValidatesAlphanumericLength(string username, bool expected) =>
        Assert.Equal(expected, UsernameRules.IsValidFormat(username));

    [Fact]
    public void Sanitize_StripsInvalidCharacters()
    {
        var result = UsernameRules.Sanitize("  Shadow-Fox!99  ");
        Assert.Equal("ShadowFox99", result);
    }
}
