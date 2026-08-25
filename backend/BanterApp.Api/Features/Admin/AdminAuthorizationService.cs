using System.Security.Claims;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

public interface IAdminAuthorizationService
{
    Task<bool> IsAdminAsync(IUserContext user, HttpContext http, CancellationToken ct = default);
    Task<bool> EnsureAdminAsync(IUserContext user, HttpContext http, CancellationToken ct = default);

    /// <summary>
    /// Checks a named capability from <see cref="AdminPermissions"/>. Unknown permission
    /// names are denied rather than defaulting open.
    /// </summary>
    Task<bool> HasPermissionAsync(
        IUserContext user,
        HttpContext http,
        string permission,
        CancellationToken ct = default);
}

public sealed class AdminAuthorizationService(
    AppDbContext db,
    IOptions<AdminOptions> options) : IAdminAuthorizationService
{
    public async Task<bool> IsAdminAsync(IUserContext user, HttpContext http, CancellationToken ct = default)
    {
        if (!user.IsAuthenticated || user.UserId is null)
        {
            return false;
        }

        var adminOptions = options.Value;
        var userId = user.UserId.Value;

        if (adminOptions.AllowedUserIds.Any(id =>
                Guid.TryParse(id, out var allowedId) && allowedId == userId))
        {
            await PromoteIfNeededAsync(userId, ct);
            return true;
        }

        var email = ResolveEmail(http);
        if (!string.IsNullOrWhiteSpace(email) &&
            adminOptions.AllowedEmails.Any(e =>
                string.Equals(e.Trim(), email, StringComparison.OrdinalIgnoreCase)))
        {
            await PromoteIfNeededAsync(userId, ct);
            return true;
        }

        var registered = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return registered?.IsPlatformAdmin == true;
    }

    public async Task<bool> HasPermissionAsync(
        IUserContext user,
        HttpContext http,
        string permission,
        CancellationToken ct = default)
    {
        if (!AdminPermissions.IsKnown(permission))
        {
            return false;
        }

        return await IsAdminAsync(user, http, ct);
    }

    public async Task<bool> EnsureAdminAsync(IUserContext user, HttpContext http, CancellationToken ct = default)
    {
        if (!await IsAdminAsync(user, http, ct))
        {
            return false;
        }

        if (user.UserId is null)
        {
            return false;
        }

        var registered = await db.Users.FirstOrDefaultAsync(u => u.Id == user.UserId.Value, ct);
        if (registered is null)
        {
            var email = ResolveEmail(http) ?? string.Empty;
            registered = new User
            {
                Id = user.UserId.Value,
                Email = email,
                DisplayName = string.IsNullOrWhiteSpace(email) ? "Admin" : email,
                IsPlatformAdmin = true
            };
            db.Users.Add(registered);
        }
        else if (!registered.IsPlatformAdmin &&
                 (IsAllowlisted(registered.Email, options.Value) ||
                  options.Value.AllowedUserIds.Any(id =>
                      Guid.TryParse(id, out var allowedId) && allowedId == registered.Id)))
        {
            registered.IsPlatformAdmin = true;
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task PromoteIfNeededAsync(Guid userId, CancellationToken ct)
    {
        var registered = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (registered is not null && !registered.IsPlatformAdmin)
        {
            registered.IsPlatformAdmin = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private static string? ResolveEmail(HttpContext http) =>
        http.User.FindFirstValue(ClaimTypes.Email)
        ?? http.User.FindFirstValue("email");

    private static bool IsAllowlisted(string email, AdminOptions adminOptions) =>
        !string.IsNullOrWhiteSpace(email) &&
        adminOptions.AllowedEmails.Any(e =>
            string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
}
