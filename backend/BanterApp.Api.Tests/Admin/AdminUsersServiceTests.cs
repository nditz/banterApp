using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Tests.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace BanterApp.Api.Tests.Admin;

/// <summary>
/// The role and status guards are the part of user management that can lock every admin
/// out of the console, so they are exercised directly against the service rather than
/// only through the endpoints.
/// </summary>
public class AdminUsersServiceTests
{
    private static readonly Guid FirstAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondAdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MemberId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task RevokeAdmin_OnSelf_IsRejected()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.RevokeAdminAsync(FirstAdminId, FirstAdminId);

        Assert.Equal(AdminUserActionOutcome.Conflict, result.Outcome);
        Assert.True(db.Users.Single(u => u.Id == FirstAdminId).IsPlatformAdmin);
    }

    [Fact]
    public async Task RevokeAdmin_OnLastRemainingAdmin_IsRejected()
    {
        using var db = CreateDb();
        db.Users.Single(u => u.Id == SecondAdminId).IsPlatformAdmin = false;
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var result = await service.RevokeAdminAsync(FirstAdminId, SecondAdminId);

        Assert.Equal(AdminUserActionOutcome.Conflict, result.Outcome);
        Assert.True(db.Users.Single(u => u.Id == FirstAdminId).IsPlatformAdmin);
    }

    [Fact]
    public async Task RevokeAdmin_WhenAnotherAdminRemains_Succeeds()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.RevokeAdminAsync(SecondAdminId, FirstAdminId);

        Assert.True(result.Success);
        Assert.False(db.Users.Single(u => u.Id == SecondAdminId).IsPlatformAdmin);
    }

    [Fact]
    public async Task RevokeAdmin_OnAllowlistedAccount_WarnsThatConfigWillRepromote()
    {
        using var db = CreateDb();
        var service = CreateService(db, allowedEmails: ["second@test.com"]);

        var result = await service.RevokeAdminAsync(SecondAdminId, FirstAdminId);

        Assert.True(result.Success);
        Assert.Contains("allowlist", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GrantAdmin_PromotesMember()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GrantAdminAsync(MemberId);

        Assert.True(result.Success);
        Assert.True(db.Users.Single(u => u.Id == MemberId).IsPlatformAdmin);
    }

    [Fact]
    public async Task GrantAdmin_ForUnknownUser_ReturnsNotFound()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GrantAdminAsync(Guid.NewGuid());

        Assert.Equal(AdminUserActionOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task SetStatus_OnSelf_IsRejected()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.SetStatusAsync(FirstAdminId, "Suspended", FirstAdminId);

        Assert.Equal(AdminUserActionOutcome.Conflict, result.Outcome);
    }

    [Fact]
    public async Task SetStatus_SuspendingAnAdmin_IsRejectedUntilRoleIsRemoved()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.SetStatusAsync(SecondAdminId, "Suspended", FirstAdminId);

        Assert.Equal(AdminUserActionOutcome.Conflict, result.Outcome);
        Assert.Equal(AccountStatus.Active, db.Users.Single(u => u.Id == SecondAdminId).AccountStatus);
    }

    [Fact]
    public async Task SetStatus_WithUnknownValue_IsRejected()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.SetStatusAsync(MemberId, "Deleted", FirstAdminId);

        Assert.Equal(AdminUserActionOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task SetStatus_OnMember_Succeeds()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.SetStatusAsync(MemberId, "suspended", FirstAdminId);

        Assert.True(result.Success);
        Assert.Equal(AccountStatus.Suspended, db.Users.Single(u => u.Id == MemberId).AccountStatus);
    }

    [Fact]
    public async Task ListUsers_ClampsPageSizeAndMatchesEmailOrDisplayNameCaseInsensitively()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var clamped = await service.ListUsersAsync(page: 1, pageSize: 5000, search: null);
        Assert.Equal(AdminUsersService.MaxPageSize, clamped.PageSize);
        Assert.Equal(3, clamped.Total);

        var byEmail = await service.ListUsersAsync(null, null, "SECOND@");
        Assert.Equal(SecondAdminId, Assert.Single(byEmail.Items).Id);

        var byName = await service.ListUsersAsync(null, null, "regular");
        Assert.Equal(MemberId, Assert.Single(byName.Items).Id);
    }

    [Fact]
    public async Task ListUsers_WithoutServiceRoleKey_ReportsDegradedIdentitySource()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var response = await service.ListUsersAsync(null, null, null);

        Assert.Equal("database", response.IdentitySource);
        Assert.NotNull(response.Warning);
    }

    private static AppDbContext CreateDb()
    {
        var db = TestDbContextFactory.Create();

        db.Users.AddRange(
            new User
            {
                Id = FirstAdminId,
                Email = "first@test.com",
                DisplayName = "First Admin",
                IsPlatformAdmin = true
            },
            new User
            {
                Id = SecondAdminId,
                Email = "second@test.com",
                DisplayName = "Second Admin",
                IsPlatformAdmin = true
            },
            new User
            {
                Id = MemberId,
                Email = "member@test.com",
                DisplayName = "Regular Member"
            });

        db.SaveChanges();
        return db;
    }

    private static AdminUsersService CreateService(
        AppDbContext db,
        List<string>? allowedEmails = null)
    {
        var options = Options.Create(new AdminOptions
        {
            AllowedEmails = allowedEmails ?? []
        });

        return new AdminUsersService(db, new UnconfiguredSupabaseAdminClient(), options);
    }

    /// <summary>Mirrors production when <c>Supabase:ServiceRoleKey</c> is unset.</summary>
    private sealed class UnconfiguredSupabaseAdminClient : ISupabaseAdminClient
    {
        public bool IsConfigured => false;

        public Task<SupabaseAdminUserPage?> ListUsersAsync(int page, int perPage, CancellationToken ct = default) =>
            Task.FromResult<SupabaseAdminUserPage?>(null);

        public Task<SupabaseAdminUser?> GetUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<SupabaseAdminUser?>(null);
    }
}
