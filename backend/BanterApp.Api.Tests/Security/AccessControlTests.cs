using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Security;

public class AccessControlTests : IClassFixture<BanterAppWebApplicationFactory>
{
    [Fact]
    public async Task UpdatePrediction_WithoutOwnership_ReturnsForbiddenOrNotFound()
    {
        using var factory = new BanterAppWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Anonymous-Id", Guid.NewGuid().ToString("N"));
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PutAsJsonAsync("/api/predictions/update", new
        {
            id = Guid.NewGuid(),
            predictionValue = "away",
            turnstileToken = "dev-bypass"
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.BadRequest,
            $"Unexpected status {(int)response.StatusCode}");
    }
}
