using BanterApp.Api.Common;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ErrorFingerprinterTests
{
    [Fact]
    public void Compute_SameInputs_ProducesSameFingerprint()
    {
        var a = ErrorFingerprinter.Compute("Development", "backend", ErrorCodes.OpenAiApiError, "HttpRequestException", "/api/ai", null, "openai", "timeout", null);
        var b = ErrorFingerprinter.Compute("Development", "backend", ErrorCodes.OpenAiApiError, "HttpRequestException", "/api/ai", null, "openai", "timeout", null);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Compute_DifferentMessages_ProducesDifferentFingerprint()
    {
        var a = ErrorFingerprinter.Compute("Development", "backend", ErrorCodes.OpenAiApiError, null, null, null, "openai", "timeout", null);
        var b = ErrorFingerprinter.Compute("Development", "backend", ErrorCodes.OpenAiApiError, null, null, null, "openai", "quota exceeded", null);
        Assert.NotEqual(a, b);
    }
}
