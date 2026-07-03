using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public class SystemLeagueServiceTests
{
    [Fact]
    public async Task EnsureSystemLeaguesAsync_SwitchesCountryLeague_WhenUserChoosesNewCountry()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var user = new UserContext { UserId = userId };

        db.Users.Add(new User { Id = userId, Email = "player@test.com", DisplayName = "Player" });
        await db.SaveChangesAsync();

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, "US", CancellationToken.None);
        await db.SaveChangesAsync();

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, "NG", CancellationToken.None);
        await db.SaveChangesAsync();

        var memberships = await db.LeagueMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.League)
            .ToListAsync();

        var countryMemberships = memberships.Where(m => m.League.Kind == LeagueKind.Country).ToList();
        Assert.Single(countryMemberships);
        Assert.Equal("NG", countryMemberships[0].League.CountryCode);

        var registered = await db.Users.FindAsync(userId);
        Assert.Equal("NG", registered!.CountryCode);
    }

    [Fact]
    public async Task EnsureSystemLeaguesAsync_RemovesCountryLeague_WhenUserChoosesGlobalOnly()
    {
        await using var db = TestDbContextFactory.Create();
        var anonId = Guid.NewGuid();
        var user = new UserContext { AnonymousUserId = anonId };

        db.AnonymousUsers.Add(new AnonymousUser
        {
            Id = anonId,
            CookieId = Guid.NewGuid().ToString("N"),
            RecoveryCode = "ABC123",
            CountryCode = "US",
        });
        await db.SaveChangesAsync();

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, "US", CancellationToken.None);
        await db.SaveChangesAsync();

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, null, CancellationToken.None);
        await db.SaveChangesAsync();

        var countryMemberships = await (
            from member in db.LeagueMembers
            join league in db.Leagues on member.LeagueId equals league.Id
            where member.AnonymousUserId == anonId && league.Kind == LeagueKind.Country
            select member).CountAsync();

        Assert.Equal(0, countryMemberships);

        var anon = await db.AnonymousUsers.FindAsync(anonId);
        Assert.Null(anon!.CountryCode);
    }
}
