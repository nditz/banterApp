using BanterApp.Api.Common;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ErrorSanitizerTests
{
    [Fact]
    public void RedactSensitiveText_RedactsAuthorizationHeader()
    {
        const string text = "Headers: Authorization: Bearer abc.def.ghi";
        var sanitized = ErrorSanitizer.RedactSensitiveText(text);
        Assert.Contains("[REDACTED]", sanitized);
        Assert.DoesNotContain("abc.def.ghi", sanitized);
    }

    [Fact]
    public void RedactSensitiveText_RedactsDatabaseUrl()
    {
        const string text = "Failed connecting to postgresql://user:pass@host/db";
        var sanitized = ErrorSanitizer.RedactSensitiveText(text);
        Assert.Contains("[REDACTED_DB_URL]", sanitized);
    }

    [Fact]
    public void SanitizeJson_TruncatesLargeBlobValues()
    {
        var json = $$"""{"payload":"{{new string('a', 600)}}"}""";
        var sanitized = ErrorSanitizer.SanitizeJson(json);
        Assert.Contains("[TRUNCATED]", sanitized);
    }
}
