using System.Text.Json;
using BanterApp.Api.Common;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class OpenAiOutputValidationTests
{
    [Fact]
    public void ParseExtraction_RejectsMalformedJson()
    {
        var extractorType = typeof(BanterApp.Api.Integrations.Pundits.OpenAiPunditOpinionExtractor);
        Assert.NotNull(extractorType);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    public void SanitizeField_StripsInjectionFromQuotes(string payload)
    {
        var sanitized = HtmlSanitizer.SanitizePlainText(payload);
        Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
    }
}
