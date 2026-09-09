using BanterApp.Api.Common;
using Xunit;

namespace BanterApp.Api.Tests.Errors;

public class ErrorCategoryMapperTests
{
    [Fact]
    public void BotBlocked_MapsToForbidden()
    {
        Assert.Equal(ErrorCodes.Forbidden, ErrorCategoryMapper.Map("bot", "bot_blocked"));
        Assert.Equal("bot", ErrorCategoryMapper.MapProvider("bot", "bot_blocked"));
        Assert.True(ErrorCategoryMapper.IsBot("bot", "bot_blocked"));
    }

    [Fact]
    public void JobSource_MapsToJobFailed()
    {
        Assert.Equal(ErrorCodes.JobFailed, ErrorCategoryMapper.Map("job", "score-sync"));
    }

    [Fact]
    public void ProviderFailure_MapsToExternalApiError()
    {
        Assert.Equal(ErrorCodes.ExternalApiError, ErrorCategoryMapper.Map("provider", "provider_failure"));
    }
}
