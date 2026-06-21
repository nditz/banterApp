using System.Security.Claims;
using BanterApp.Api.Common;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

public class AdminAuthorizationServiceTests
{
    private static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RegularUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task IsAdminAsync_AllowlistedEmail_ReturnsTrue()
    {
        await using var db = TestDbContextFactory.Create();
        var service = CreateService(db, allowedEmails: ["admin@test.com"]);
        var http = CreateHttpContext(AdminUserId, "admin@test.com");
        var user = new UserContext { UserId = AdminUserId };

        var isAdmin = await service.IsAdminAsync(user, http);

        Assert.True(isAdmin);
    }

    [Fact]
    public async Task IsAdminAsync_PlatformAdminFlag_ReturnsTrue()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = RegularUserId,
            Email = "user@test.com",
            DisplayName = "User",
            IsPlatformAdmin = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var http = CreateHttpContext(RegularUserId, "user@test.com");
        var user = new UserContext { UserId = RegularUserId };

        Assert.True(await service.IsAdminAsync(user, http));
    }

    [Fact]
    public async Task IsAdminAsync_AnonymousUser_ReturnsFalse()
    {
        await using var db = TestDbContextFactory.Create();
        var service = CreateService(db, allowedEmails: ["admin@test.com"]);
        var http = new DefaultHttpContext();
        var user = new UserContext { AnonymousUserId = Guid.NewGuid() };

        Assert.False(await service.IsAdminAsync(user, http));
    }

    [Fact]
    public async Task IsAdminAsync_NotAllowlistedAndNotFlag_ReturnsFalse()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User
        {
            Id = RegularUserId,
            Email = "user@test.com",
            DisplayName = "User"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, allowedEmails: ["admin@test.com"]);
        var http = CreateHttpContext(RegularUserId, "user@test.com");
        var user = new UserContext { UserId = RegularUserId };

        Assert.False(await service.IsAdminAsync(user, http));
    }

    [Fact]
    public async Task EnsureAdminAsync_AllowlistedEmail_PromotesUserInDatabase()
    {
        await using var db = TestDbContextFactory.Create();
        var service = CreateService(db, allowedEmails: ["admin@test.com"]);
        var http = CreateHttpContext(AdminUserId, "admin@test.com");
        var user = new UserContext { UserId = AdminUserId };

        Assert.True(await service.EnsureAdminAsync(user, http));

        var saved = await db.Users.FindAsync(AdminUserId);
        Assert.NotNull(saved);
        Assert.True(saved!.IsPlatformAdmin);
        Assert.Equal("admin@test.com", saved.Email);
    }

    private static AdminAuthorizationService CreateService(
        BanterApp.Api.Data.AppDbContext db,
        string[]? allowedEmails = null,
        string[]? allowedUserIds = null) =>
        new(db, Options.Create(new AdminOptions
        {
            AllowedEmails = allowedEmails?.ToList() ?? [],
            AllowedUserIds = allowedUserIds?.ToList() ?? []
        }));

    private static DefaultHttpContext CreateHttpContext(Guid userId, string email)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        ], authenticationType: "test"));
        return http;
    }
}
