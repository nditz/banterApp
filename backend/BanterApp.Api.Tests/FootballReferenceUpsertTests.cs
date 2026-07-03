using BanterApp.Api.Data.Entities;
using BanterApp.Api.Integrations.FootballReference;
using BanterApp.Api.Integrations.FootballReference.Dtos;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class FootballReferenceUpsertTests
{
    [Fact]
    public async Task UpsertCountriesAsync_InsertsAndUpdatesByExternalId()
    {
        await using var db = TestDbContextFactory.Create();
        var upsert = new ReferenceDataUpsertService(db);

        var items = new List<CountryDto>
        {
            new("1", "France", "FR", "https://flag/fr", "Europe", 3, null),
            new("2", "Brazil", "BR", null, "South America", 5, null)
        };

        var (created, updated) = await upsert.UpsertCountriesAsync(items, "api_sports");
        Assert.Equal(2, created);
        Assert.Equal(0, updated);

        items[0] = new CountryDto("1", "France Updated", "FR", "https://flag/fr", "Europe", 2, null);
        (created, updated) = await upsert.UpsertCountriesAsync(items.Take(1).ToList(), "api_sports");
        Assert.Equal(0, created);
        Assert.Equal(1, updated);

        var france = await db.Countries.SingleAsync(c => c.ExternalId == "1");
        Assert.Equal("France Updated", france.Name);
        Assert.Equal(2, france.FifaRanking);
    }

    [Fact]
    public async Task UpsertPlayersAsync_LinksCountryByExternalId()
    {
        await using var db = TestDbContextFactory.Create();
        var upsert = new ReferenceDataUpsertService(db);

        await upsert.UpsertCountriesAsync(
            [new CountryDto("10", "France", "FR", null, null, null, null)],
            "api_sports");

        var countryId = await db.Countries.Where(c => c.ExternalId == "10").Select(c => c.Id).SingleAsync();

        var (created, _) = await upsert.UpsertPlayersAsync(
            [new PlayerDto("100", "10", "Kylian", "Mbappe", "Kylian Mbappe", null, null, 25, "Forward", null, "PSG", "France", null)],
            "api_sports");

        Assert.Equal(1, created);
        var player = await db.Players.Include(p => p.Country).SingleAsync();
        Assert.Equal(countryId, player.CountryId);
        Assert.Equal("Kylian Mbappe", player.DisplayName);
    }

    [Fact]
    public async Task UpsertLeaderboardAsync_CreatesTopScorerEntries()
    {
        await using var db = TestDbContextFactory.Create();
        var upsert = new ReferenceDataUpsertService(db);

        await upsert.UpsertCountriesAsync(
            [new CountryDto("10", "France", "FR", null, null, null, null)],
            "api_sports");
        await upsert.UpsertPlayersAsync(
            [new PlayerDto("100", "10", null, null, "Kylian Mbappe", null, null, null, null, null, null, "France", null)],
            "api_sports");

        var (created, _) = await upsert.UpsertLeaderboardAsync(
            [new LeaderboardEntryDto("100", "10", 1, 5, null)],
            LeaderboardTypes.TopScorers,
            "api_sports",
            "WC",
            "2026");

        Assert.Equal(1, created);
        var entry = await db.LeaderboardEntries.SingleAsync();
        Assert.Equal(LeaderboardTypes.TopScorers, entry.LeaderboardType);
        Assert.Equal(5, entry.Value);
        Assert.Equal(1, entry.Rank);
    }

    [Fact]
    public async Task UpsertLeaderboardAsync_ReplacesExistingTopAssistsForSeason()
    {
        await using var db = TestDbContextFactory.Create();
        var upsert = new ReferenceDataUpsertService(db);

        await upsert.UpsertCountriesAsync(
            [new CountryDto("10", "France", "FR", null, null, null, null)],
            "api_sports");
        await upsert.UpsertPlayersAsync(
            [
                new PlayerDto("100", "10", null, null, "Player A", null, null, null, null, null, null, "France", null),
                new PlayerDto("101", "10", null, null, "Player B", null, null, null, null, null, null, "France", null)
            ],
            "api_sports");

        await upsert.UpsertLeaderboardAsync(
            [new LeaderboardEntryDto("100", "10", 1, 7, null)],
            LeaderboardTypes.TopAssists,
            "api_sports",
            "WC",
            "2026");

        var (created, updated) = await upsert.UpsertLeaderboardAsync(
            [
                new LeaderboardEntryDto("101", "10", 1, 9, null),
                new LeaderboardEntryDto("100", "10", 2, 7, null)
            ],
            LeaderboardTypes.TopAssists,
            "api_sports",
            "WC",
            "2026");

        Assert.Equal(1, created);
        Assert.Equal(1, updated);

        var entries = await db.LeaderboardEntries
            .Where(e => e.LeaderboardType == LeaderboardTypes.TopAssists)
            .OrderBy(e => e.Rank)
            .ToListAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal(9, entries[0].Value);
        Assert.Equal(7, entries[1].Value);
    }
}
