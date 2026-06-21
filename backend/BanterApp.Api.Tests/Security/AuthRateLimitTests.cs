using System.Net;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class AuthRateLimitTests : IClassFixture<BanterAppWebApplicationFactory>
{
    [Fact]
    public async Task Login_EndpointExists_AndRejectsWithoutBody()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
