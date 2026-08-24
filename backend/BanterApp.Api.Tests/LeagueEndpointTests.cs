using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Tests.Infrastructure;
using Xunit;

namespace BanterApp.Api.Tests;

public class LeagueEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public LeagueEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_ThenAnotherGuestJoinsWithInviteCode()
    {
        using var host = await _factory.CreateConsentedAnonymousClientAsync();
        var created = await host.PostAsJsonAsync("/api/leagues/create", new { name = "Office Sweep" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var league = await created.Content.ReadFromJsonAsync<CreatedLeague>();
        Assert.False(string.IsNullOrWhiteSpace(league?.InviteCode));

        using var guest = await _factory.CreateConsentedAnonymousClientAsync();
        var joined = await guest.PostAsJsonAsync("/api/leagues/join", new { inviteCode = league!.InviteCode });
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);

        var mine = await guest.GetAsync("/api/leagues");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var body = await mine.Content.ReadAsStringAsync();
        Assert.Contains("Office Sweep", body, StringComparison.Ordinal);
        Assert.Contains(league.InviteCode, body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CreatedLeague(string InviteCode);
}
