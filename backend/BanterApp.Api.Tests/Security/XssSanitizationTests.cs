using BanterApp.Api.Common;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class XssSanitizationTests
{
    [Theory]
    [InlineData("<script>alert(1)</script>hello")]
    [InlineData("javascript:alert(1)")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("<svg onload=alert(1)>")]
    public void SanitizePlainText_RemovesDangerousMarkup(string input)
    {
        var sanitized = HtmlSanitizer.SanitizePlainText(input);

        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContainsDangerousMarkup_DetectsScriptTag()
    {
        Assert.True(HtmlSanitizer.ContainsDangerousMarkup("<script>alert(1)</script>"));
    }
}
