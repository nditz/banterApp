using BanterApp.Api.Common;
using BanterApp.Api.Integrations.Common;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ProviderErrorMapperTests
{
    [Fact]
    public void MapOpenAi_RateLimit_IsRetryable()
    {
        var ex = ProviderErrorMapper.MapOpenAi(429, "complete", "gpt-4o-mini");
        Assert.Equal(ErrorCodes.RateLimited, ex.Code);
        Assert.True(ex.IsRetryable);
        Assert.Equal(StatusCodes.Status429TooManyRequests, ex.StatusCode);
    }

    [Fact]
    public void MapYouTube_Forbidden_IsNotRetryable()
    {
        var ex = ProviderErrorMapper.MapYouTube(403, "search", channelId: "abc");
        Assert.Equal(ErrorCodes.YouTubeApiError, ex.Code);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public void MapRss_ContentTypeFailure_IsNotSsrfMessage()
    {
        var ex = ProviderErrorMapper.MapRss("invalid_xml", feedUrl: "https://example.com/feed");
        Assert.Equal(ErrorCodes.RssFetchError, ex.Code);
        Assert.Equal("Feed returned invalid content.", ex.SafeMessage);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public void MapRss_SsrfBlocked_IsNotRetryable()
    {
        var ex = ProviderErrorMapper.MapRss("blocked", ssrfBlocked: true, feedUrl: "http://127.0.0.1/feed");
        Assert.Equal(ErrorCodes.RssFetchError, ex.Code);
        Assert.Equal("Feed URL is not allowed.", ex.SafeMessage);
        Assert.False(ex.IsRetryable);
    }

    [Fact]
    public void ComputeRetryDelaySeconds_CapsAtOneHour()
    {
        Assert.True(ProviderErrorMapper.ComputeRetryDelaySeconds(20) <= 3600);
    }
}
