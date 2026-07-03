using System.Net;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class FootballReferenceAdminTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public FootballReferenceAdminTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task SyncCountries_NonAdmin_ReturnsForbidden()
    {
        using var client = _factory.CreateNonAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync("/api/admin/football-data/sync/countries", null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SyncCountries_Admin_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var response = await client.PostAsync("/api/admin/football-data/sync/countries", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFootballOverview_Admin_ReturnsOk()
    {
        using var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/admin/football-data/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
