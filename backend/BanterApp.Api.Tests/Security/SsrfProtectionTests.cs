using Microsoft.Extensions.Configuration;
using BanterApp.Api.Services;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class SsrfProtectionTests
{
    [Theory]
    [InlineData("http://127.0.0.1/secret")]
    [InlineData("http://localhost/admin")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://192.168.1.1/router")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("file:///etc/passwd")]
    public async Task ValidateAsync_BlocksUnsafeUrls(string url)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var validator = new OutboundUrlValidator(config, new NoOpErrorLogger());

        var result = await validator.ValidateAsync(url);

        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void IsPrivateOrReserved_BlocksLoopback()
    {
        Assert.True(OutboundUrlValidator.IsPrivateOrReserved(System.Net.IPAddress.Loopback));
    }

    private sealed class NoOpErrorLogger : IApplicationErrorLogger
    {
        public Task LogAsync(string source, string message, string? category = null, string? detail = null,
            string? requestMethod = null, string? requestPath = null, int? statusCode = null,
            Guid? syncRunId = null, CancellationToken ct = default) => Task.CompletedTask;

        public Task LogExceptionAsync(string source, Exception exception, string? category = null,
            string? requestMethod = null, string? requestPath = null,
            int? statusCode = null, Guid? syncRunId = null, CancellationToken ct = default) => Task.CompletedTask;
    }
}
