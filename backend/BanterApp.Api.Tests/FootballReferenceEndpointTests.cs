using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballReferenceEndpointTests : IClassFixture<BanterAppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly BanterAppWebApplicationFactory _factory;

    public FootballReferenceEndpointTests(BanterAppWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCountries_ReturnsSeededActiveCountries()
    {
        var franceId = Guid.NewGuid();
        await SeedReferenceDataAsync(franceId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/countries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CountriesPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains(payload!.Countries, c => c.Name == "France");
        Assert.DoesNotContain(payload.Countries, c => c.Name == "Inactive Land");
    }

    [Fact]
    public async Task GetCountries_SearchFiltersByName()
    {
        await SeedReferenceDataAsync(Guid.NewGuid());

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/countries?search=bra");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CountriesPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Single(payload!.Countries);
        Assert.Equal("Brazil", payload.Countries[0].Name);
    }

    [Fact]
    public async Task GetCountries_DedupesDuplicateCountryCodes()
    {
        var francePrimaryId = Guid.NewGuid();
        var franceDuplicateId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await EnsureCountryAsync(db, francePrimaryId, "France", "FR", isActive: true);
            await EnsureCountryAsync(db, franceDuplicateId, "France (dup)", "FR", isActive: true);
        }

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/countries");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CountriesPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Single(payload!.Countries, c => c.Code == "FR");
    }

    private static readonly Guid TestPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    [Fact]
    public async Task GetPlayers_SearchFiltersByDisplayName()
    {
        var franceId = Guid.NewGuid();
        await SeedReferenceDataAsync(franceId, TestPlayerId);

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/players?search=mbapp");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PlayersPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Single(payload!.Players);
        Assert.Equal("Kylian Mbappe", payload.Players[0].DisplayName);
    }

    [Fact]
    public async Task GetTopScorers_ReturnsLeaderboardEntries()
    {
        var franceId = Guid.NewGuid();
        await SeedReferenceDataAsync(franceId, TestPlayerId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LeaderboardEntries.Add(new LeaderboardEntry
        {
            Id = Guid.NewGuid(),
            LeaderboardType = LeaderboardTypes.TopScorers,
            PlayerId = TestPlayerId,
            CountryId = franceId,
            Rank = 1,
            Value = 5,
            Competition = "PL",
            Season = "2026",
            SourceProvider = "test"
        });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/football/leaderboards/top-scorers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LeaderboardPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Single(payload!.Entries);
        Assert.Equal(1, payload.Entries[0].Rank);
        Assert.Equal(5, payload.Entries[0].Value);
    }

    private async Task SeedReferenceDataAsync(Guid franceId, Guid? playerId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureCountryAsync(db, franceId, "France", "FR", true);
        await EnsureCountryAsync(db, BrazilCountryId, "Brazil", "BR", true);
        await EnsureCountryAsync(db, InactiveCountryId, "Inactive Land", "XX", false);

        if (playerId is not null)
        {
            if (!await db.Players.AnyAsync(p => p.Id == playerId.Value))
            {
                db.Players.Add(new Player
                {
                    Id = playerId.Value,
                    DisplayName = "Kylian Mbappe",
                    CountryId = franceId,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private static readonly Guid BrazilCountryId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid InactiveCountryId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static async Task EnsureCountryAsync(
        AppDbContext db,
        Guid id,
        string name,
        string code,
        bool isActive)
    {
        if (await db.Countries.AnyAsync(c => c.Id == id))
        {
            return;
        }

        db.Countries.Add(new Country { Id = id, Name = name, Code = code, IsActive = isActive });
        await db.SaveChangesAsync();
    }

    private sealed record CountriesPayload(List<CountryItem> Countries);

    private sealed record CountryItem(Guid Id, string Name, string? Code);

    private sealed record PlayersPayload(List<PlayerItem> Players);

    private sealed record PlayerItem(Guid Id, string DisplayName);

    private sealed record LeaderboardPayload(List<LeaderboardItem> Entries);

    private sealed record LeaderboardItem(int? Rank, decimal Value, string PlayerName);
}
