using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Opinions;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanterApp.Api.Tests;

public sealed class PunditFollowServiceTests
{
    [Fact]
    public async Task FollowAsync_persists_source_pundit_for_anonymous_session()
    {
        await using var db = TestDbContextFactory.Create();
        var pundit = await SeedSourcePunditAsync(db);
        var user = new UserContext { AnonymousUserId = Guid.NewGuid() };
        var service = new PunditFollowService(db);

        var (dto, error, status) = await service.FollowAsync(pundit.Id, user, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(StatusCodes.Status201Created, status);
        Assert.True(dto!.IsFollowed);
        Assert.Equal(1, await db.PunditFollows.CountAsync());
        Assert.Contains(pundit.Id, await service.GetFollowedPunditIdsAsync(user, CancellationToken.None));
    }

    [Fact]
    public async Task FollowAsync_rejects_persona_desks()
    {
        await using var db = TestDbContextFactory.Create();
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Persona,
            Name = "Parody Desk",
            NormalizedName = "parody desk"
        };
        db.Pundits.Add(pundit);
        await db.SaveChangesAsync();
        var service = new PunditFollowService(db);

        var (_, error, status) = await service.FollowAsync(
            pundit.Id,
            new UserContext { UserId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Contains("sourced", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.PunditFollows.CountAsync());
    }

    [Fact]
    public async Task FollowAsync_is_idempotent()
    {
        await using var db = TestDbContextFactory.Create();
        var pundit = await SeedSourcePunditAsync(db);
        var user = new UserContext { UserId = Guid.NewGuid() };
        var service = new PunditFollowService(db);

        await service.FollowAsync(pundit.Id, user, CancellationToken.None);
        var (_, error, status) = await service.FollowAsync(pundit.Id, user, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(1, await db.PunditFollows.CountAsync());
    }

    [Fact]
    public async Task UnfollowAsync_removes_row()
    {
        await using var db = TestDbContextFactory.Create();
        var pundit = await SeedSourcePunditAsync(db);
        var user = new UserContext { UserId = Guid.NewGuid() };
        var service = new PunditFollowService(db);
        await service.FollowAsync(pundit.Id, user, CancellationToken.None);

        await service.UnfollowAsync(pundit.Id, user, CancellationToken.None);

        Assert.Equal(0, await db.PunditFollows.CountAsync());
    }

    [Fact]
    public async Task ListDirectoryAsync_marks_followed_and_keeps_attribution()
    {
        await using var db = TestDbContextFactory.Create();
        var pundit = await SeedSourcePunditAsync(db);
        var user = new UserContext { AnonymousUserId = Guid.NewGuid() };
        var service = new PunditFollowService(db);
        await service.FollowAsync(pundit.Id, user, CancellationToken.None);

        var directory = await service.ListDirectoryAsync(PunditKind.Source, 20, user, CancellationToken.None);

        var row = Assert.Single(directory);
        Assert.True(row.IsFollowed);
        Assert.False(row.IsFictionalPersona);
        Assert.False(string.IsNullOrWhiteSpace(row.AttributionNote));
        Assert.Equal("https://example.com/neville", row.SourceUrl);
    }

    private static async Task<Pundit> SeedSourcePunditAsync(AppDbContext db)
    {
        var pundit = new Pundit
        {
            Id = Guid.NewGuid(),
            Kind = PunditKind.Source,
            Name = "Gary Neville",
            NormalizedName = "gary neville",
            Organization = "Sky Sports",
            AttributionMode = PunditAttributionMode.Licensed,
            SourceUrl = "https://example.com/neville"
        };
        db.Pundits.Add(pundit);
        await db.SaveChangesAsync();
        return pundit;
    }
}
