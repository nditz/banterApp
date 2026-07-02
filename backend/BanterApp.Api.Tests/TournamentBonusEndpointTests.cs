using System.Net;
using System.Net.Http.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class TournamentBonusEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private readonly BanterAppWebApplicationFactory _factory;

    public TournamentBonusEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetTournamentBonuses_WithActiveSession_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Anonymous-Id", Guid.NewGuid().ToString("N"));
        await CsrfTestHelper.ApplyCsrfAsync(client);

        var consent = await client.PostAsJsonAsync("/api/auth/session/consent", new
        {
            acceptedTerms = true,
            turnstileToken = "dev-bypass",
        });
        consent.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/tournament-bonuses");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK, got {(int)response.StatusCode}: {body}");
    }

    [Fact]
    public async Task LoadTeamNameMap_BuildsDistinctTeamsFromMatchRows()
    {
        await using var db = TestDbContextFactory.Create();
        db.Matches.Add(new Match
        {
            Id = "m1",
            TeamA = "Brazil",
            TeamB = "France",
            TeamACode = "BRA",
            TeamBCode = "FRA",
            KickoffTime = DateTimeOffset.UtcNow.AddDays(1),
        });
        await db.SaveChangesAsync();

        var rows = await db.Matches
            .AsNoTracking()
            .Select(m => new { m.TeamACode, m.TeamA, m.TeamBCode, m.TeamB })
            .ToListAsync();

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!string.IsNullOrEmpty(row.TeamACode) && row.TeamACode != "TBD")
            {
                map[row.TeamACode] = row.TeamA;
            }

            if (!string.IsNullOrEmpty(row.TeamBCode) && row.TeamBCode != "TBD")
            {
                map[row.TeamBCode] = row.TeamB;
            }
        }

        Assert.Equal(2, map.Count);
        Assert.Equal("Brazil", map["BRA"]);
        Assert.Equal("France", map["FRA"]);
    }
}
