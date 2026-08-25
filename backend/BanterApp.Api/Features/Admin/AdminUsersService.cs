using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public sealed class AdminUsersService(
    AppDbContext db,
    ISupabaseAdminClient supabase,
    IOptions<AdminOptions> adminOptions)
{
    public const int MaxPageSize = 100;
    public const string AdminRole = "admin";

    /// <summary>
    /// Lists application users. Pagination and search run against the local
    /// <c>users</c> table, which mirrors every account that has signed in, so results
    /// stay searchable and role/status data is always authoritative. Supabase identity
    /// detail (last sign-in, providers) is attached per user by
    /// <see cref="GetUserAsync"/> rather than costing one request per row here.
    /// </summary>
    public async Task<AdminUserListResponse> ListUsersAsync(
        int? page,
        int? pageSize,
        string? search,
        CancellationToken ct = default)
    {
        var currentPage = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, MaxPageSize);

        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Lowered comparison rather than ILike so the same query runs on the
            // in-memory provider used by the integration tests.
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term) ||
                u.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(u => new AdminUserListItem(
                u.Id,
                u.Email,
                u.DisplayName,
                u.CountryCode,
                u.AccountStatus.ToString(),
                u.IsPlatformAdmin,
                u.EmailConfirmedAt,
                u.TermsAcceptedAt,
                u.CreatedAt,
                db.Predictions.Count(p => p.UserId == u.Id),
                db.LeagueMembers.Count(m => m.UserId == u.Id)))
            .ToListAsync(ct);

        return new AdminUserListResponse(
            items,
            currentPage,
            size,
            total,
            supabase.IsConfigured ? "supabase" : "database",
            supabase.IsConfigured
                ? null
                : "Supabase service-role key is not configured. Sign-in history and login "
                  + "providers are unavailable; showing application database records only.");
    }

    public async Task<AdminUserDetail?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        var predictionCount = await db.Predictions.CountAsync(p => p.UserId == userId, ct);
        var generatedContentCount = await db.GeneratedContents.CountAsync(g => g.UserId == userId, ct);
        var lastPredictionAt = await db.Predictions
            .Where(p => p.UserId == userId)
            .MaxAsync(p => (DateTimeOffset?)p.CreatedAt, ct);

        var leagues = await db.LeagueMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.League.Name)
            .Take(50)
            .Select(m => new AdminUserLeagueMembership(
                m.LeagueId,
                m.League.Name,
                m.League.Kind.ToString(),
                m.IsAdmin))
            .ToListAsync(ct);

        var identity = await supabase.GetUserAsync(userId, ct);

        return new AdminUserDetail(
            user.Id,
            user.Email,
            user.DisplayName,
            user.CountryCode,
            user.Avatar,
            user.AccountStatus.ToString(),
            user.IsPlatformAdmin,
            IsAllowlisted(user),
            user.EmailConfirmedAt,
            user.TermsAcceptedAt,
            user.CreatedAt,
            identity is null
                ? null
                : new AdminUserIdentity(
                    identity.CreatedAt,
                    identity.LastSignInAt,
                    identity.EmailConfirmedAt,
                    identity.Providers,
                    identity.BannedUntil is not null),
            new AdminUserActivity(
                predictionCount,
                leagues.Count,
                generatedContentCount,
                lastPredictionAt),
            leagues,
            supabase.IsConfigured ? "supabase" : "database");
    }

    public async Task<AdminUserActionResult> GrantAdminAsync(
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (user is null)
        {
            return AdminUserActionResult.NotFound("User not found.");
        }

        if (user.IsPlatformAdmin)
        {
            return AdminUserActionResult.Ok("User is already a platform admin.");
        }

        user.IsPlatformAdmin = true;
        await db.SaveChangesAsync(ct);
        return AdminUserActionResult.Ok();
    }

    public async Task<AdminUserActionResult> RevokeAdminAsync(
        Guid targetUserId,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        if (targetUserId == actingUserId)
        {
            return AdminUserActionResult.Conflict(
                "You cannot remove your own admin role. Ask another admin to do it.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (user is null)
        {
            return AdminUserActionResult.NotFound("User not found.");
        }

        if (!user.IsPlatformAdmin)
        {
            return AdminUserActionResult.Ok("User is not a platform admin.");
        }

        var adminCount = await db.Users.CountAsync(u => u.IsPlatformAdmin, ct);
        if (adminCount <= 1)
        {
            return AdminUserActionResult.Conflict(
                "This is the last platform admin. Grant the role to another account first.");
        }

        user.IsPlatformAdmin = false;
        await db.SaveChangesAsync(ct);

        return IsAllowlisted(user)
            ? AdminUserActionResult.Ok(
                "Role removed, but this account is still in the Admin allowlist configuration "
                + "and will be promoted again on its next request. Remove it from "
                + "Admin__AllowedEmails / Admin__AllowedUserIds to make this permanent.")
            : AdminUserActionResult.Ok();
    }

    public async Task<AdminUserActionResult> SetStatusAsync(
        Guid targetUserId,
        string status,
        Guid actingUserId,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            return AdminUserActionResult.Invalid(
                $"Unknown account status. Expected one of: {string.Join(", ", Enum.GetNames<AccountStatus>())}.");
        }

        if (targetUserId == actingUserId)
        {
            return AdminUserActionResult.Conflict("You cannot change your own account status.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, ct);
        if (user is null)
        {
            return AdminUserActionResult.NotFound("User not found.");
        }

        if (user.IsPlatformAdmin && parsed is AccountStatus.Suspended or AccountStatus.Banned)
        {
            return AdminUserActionResult.Conflict(
                "Remove the platform admin role before suspending or banning this account.");
        }

        if (user.AccountStatus == parsed)
        {
            return AdminUserActionResult.Ok($"Account status is already {parsed}.");
        }

        user.AccountStatus = parsed;
        await db.SaveChangesAsync(ct);
        return AdminUserActionResult.Ok();
    }

    private bool IsAllowlisted(User user)
    {
        var options = adminOptions.Value;

        if (options.AllowedUserIds.Any(id =>
                Guid.TryParse(id, out var allowedId) && allowedId == user.Id))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(user.Email) &&
               options.AllowedEmails.Any(e =>
                   string.Equals(e.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public enum AdminUserActionOutcome
{
    Success,
    NotFound,
    Invalid,
    Conflict
}

public sealed record AdminUserActionResult(
    AdminUserActionOutcome Outcome,
    string? Message)
{
    public bool Success => Outcome == AdminUserActionOutcome.Success;

    public static AdminUserActionResult Ok(string? message = null) =>
        new(AdminUserActionOutcome.Success, message);

    public static AdminUserActionResult NotFound(string message) =>
        new(AdminUserActionOutcome.NotFound, message);

    public static AdminUserActionResult Invalid(string message) =>
        new(AdminUserActionOutcome.Invalid, message);

    public static AdminUserActionResult Conflict(string message) =>
        new(AdminUserActionOutcome.Conflict, message);
}

public sealed record AdminUserListItem(
    Guid Id,
    string Email,
    string DisplayName,
    string? CountryCode,
    string AccountStatus,
    bool IsPlatformAdmin,
    DateTimeOffset? EmailConfirmedAt,
    DateTimeOffset? TermsAcceptedAt,
    DateTimeOffset CreatedAt,
    int PredictionCount,
    int LeagueCount);

public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserListItem> Items,
    int Page,
    int PageSize,
    int Total,
    string IdentitySource,
    string? Warning);

public sealed record AdminUserIdentity(
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastSignInAt,
    DateTimeOffset? EmailConfirmedAt,
    IReadOnlyList<string> Providers,
    bool IsBanned);

public sealed record AdminUserActivity(
    int PredictionCount,
    int LeagueCount,
    int GeneratedContentCount,
    DateTimeOffset? LastPredictionAt);

public sealed record AdminUserLeagueMembership(
    Guid LeagueId,
    string Name,
    string Kind,
    bool IsLeagueAdmin);

public sealed record AdminUserDetail(
    Guid Id,
    string Email,
    string DisplayName,
    string? CountryCode,
    string? Avatar,
    string AccountStatus,
    bool IsPlatformAdmin,
    bool IsAllowlisted,
    DateTimeOffset? EmailConfirmedAt,
    DateTimeOffset? TermsAcceptedAt,
    DateTimeOffset CreatedAt,
    AdminUserIdentity? Identity,
    AdminUserActivity Activity,
    IReadOnlyList<AdminUserLeagueMembership> Leagues,
    string IdentitySource);
