using BanterApp.Api.Features.Admin;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class SecretSanitizerTests
{
    [Fact]
    public void SanitizeJson_RedactsSensitiveKeys()
    {
        const string json = """{"api_key":"sk-secret","title":"hello","nested":{"access_token":"abc"}}""";

        var sanitized = SecretSanitizer.SanitizeJson(json);

        Assert.Contains("[REDACTED]", sanitized);
        Assert.DoesNotContain("sk-secret", sanitized);
        Assert.DoesNotContain("abc", sanitized);
        Assert.Contains("hello", sanitized);
    }

    [Fact]
    public void SanitizeJson_PreservesNonSensitiveFields()
    {
        const string json = """{"job_name":"rss.sync","items_processed":12}""";

        var sanitized = SecretSanitizer.SanitizeJson(json);

        Assert.Contains("rss.sync", sanitized);
        Assert.Contains("12", sanitized);
        Assert.DoesNotContain("[REDACTED]", sanitized);
    }

    [Fact]
    public void RedactSensitiveText_RedactsInlineSecrets()
    {
        const string text = "Failed: api_key=super-secret-value here";

        var sanitized = SecretSanitizer.RedactSensitiveText(text);

        Assert.Contains("[REDACTED]", sanitized);
        Assert.DoesNotContain("super-secret-value", sanitized);
    }

    [Fact]
    public void SanitizeJson_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SecretSanitizer.SanitizeJson(null));
        Assert.Equal(string.Empty, SecretSanitizer.SanitizeJson(""));
    }
}
