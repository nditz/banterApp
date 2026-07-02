using System.Security.Claims;
using BanterApp.Api.Common;
using BanterApp.Api.Data;using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Admin;
using BanterApp.Api.Features.Leagues;
using BanterApp.Api.Middleware;
using BanterApp.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Features.Auth;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth/session").WithTags("Auth");

        group.MapGet("/", GetSession);
        group.MapPost("/consent", AcceptTerms)
            .RequireRateLimiting(RateLimitPolicies.AuthSession)
            .WithValidation<SessionConsentRequest>();
        group.MapPost("/recover", RecoverSession)
            .RequireRateLimiting(RateLimitPolicies.AuthSession)
            .WithValidation<SessionRecoverRequest>();
        group.MapPost("/sync", SyncAuthenticatedUser);

        return app;
    }

    /// <summary>Upserts the app user row after Supabase OAuth (Google, etc.).</summary>
    private static async Task<IResult> SyncAuthenticatedUser(
        IUserContext user,
        HttpContext http,
        AppDbContext db,
        CancellationToken ct)
    {
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var userId = user.UserId!.Value;
        var email = http.User.FindFirstValue(ClaimTypes.Email)
                    ?? http.User.FindFirstValue("email")
                    ?? string.Empty;

        var existing = await db.Users.FindAsync([userId], ct);
        if (existing is null)
        {
            db.Users.Add(new User
            {
                Id = userId,
                Email = email,
                DisplayName = string.IsNullOrWhiteSpace(email) ? "Player" : email
            });
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            existing.Email = email;
            if (string.IsNullOrWhiteSpace(existing.DisplayName) || existing.DisplayName == "Player")
            {
                existing.DisplayName = email;
            }
        }

        await db.SaveChangesAsync(ct);

        // Do not derive a country league from the browser-locale header; only keep the
        // Country league the user explicitly chose earlier (persisted on their record).
        await SystemLeagueService.EnsureSystemLeaguesForSessionAsync(db, user, ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { synced = true, userId });
    }

    private static async Task<IResult> GetSession(
        IUserContext user,
        SessionTokenService tokens,
        AppDbContext db,
        IAdminAuthorizationService adminAuth,
        HttpContext http,
        CancellationToken ct)
    {
        var csrf = CsrfMiddleware.IssueToken(http);

        if (user.IsAuthenticated)
        {
            var registered = await db.Users.FindAsync([user.UserId!.Value], ct);
            var isPlatformAdmin = await adminAuth.IsAdminAsync(user, http, ct);
            return Results.Ok(new SessionResponse(
                Authenticated: true,
                Anonymous: false,
                TermsAccepted: registered?.TermsAcceptedAt is not null,
                RecoveryToken: null,
                UserId: user.UserId,
                AnonymousUserId: null,
                CsrfToken: csrf,
                IsPlatformAdmin: isPlatformAdmin));
        }

        if (user.IsAnonymous)
        {
            var anonymous = http.Items["AnonymousUser"] as AnonymousUser;
            var recoveryToken = anonymous?.TermsAcceptedAt is not null
                ? tokens.CreateRecoveryToken(anonymous.Id)
                : null;

            return Results.Ok(new SessionResponse(
                Authenticated: false,
                Anonymous: true,
                TermsAccepted: anonymous?.TermsAcceptedAt is not null,
                RecoveryToken: recoveryToken,
                UserId: null,
                AnonymousUserId: user.AnonymousUserId,
                CsrfToken: csrf,
                IsPlatformAdmin: false));
        }

        return Results.Ok(new SessionResponse(
            Authenticated: false,
            Anonymous: false,
            TermsAccepted: false,
            RecoveryToken: null,
            UserId: null,
            AnonymousUserId: null,
            CsrfToken: csrf,
            IsPlatformAdmin: false));
    }

    private static async Task<IResult> AcceptTerms(
        SessionConsentRequest request,
        AppDbContext db,
        IUserContext user,
        SessionTokenService tokens,
        TurnstileService turnstile,
        IAdminAuthorizationService adminAuth,
        HttpContext http,
        CancellationToken ct)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        // The country league is opt-in: use the explicit choice from the terms screen only.
        // A null/blank value means the user did not pick one -> Global league only.
        var chosenCountry = SystemLeagueService.NormalizeCountryCodeOrNull(request.CountryCode);

        if (user.IsAuthenticated)
        {
            var registered = await db.Users.FindAsync([user.UserId!.Value], ct);
            if (registered is null)
            {
                registered = new User
                {
                    Id = user.UserId!.Value,
                    Email = string.Empty,
                    DisplayName = "Player",
                    TermsAcceptedAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(registered);
            }
            else if (registered.TermsAcceptedAt is null)
            {
                registered.TermsAcceptedAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            var csrf = CsrfMiddleware.IssueToken(http);

            await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, chosenCountry, ct);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new SessionResponse(
                Authenticated: true,
                Anonymous: false,
                TermsAccepted: true,
                RecoveryToken: null,
                UserId: user.UserId,
                AnonymousUserId: null,
                CsrfToken: csrf,
                IsPlatformAdmin: await adminAuth.IsAdminAsync(user, http, ct)));
        }

        var cookieId = ResolveCookieId(http);
        var anonymous = await db.AnonymousUsers
            .FirstOrDefaultAsync(a => a.CookieId == cookieId, ct);

        if (anonymous is null)
        {
            anonymous = new AnonymousUser
            {
                Id = Guid.NewGuid(),
                CookieId = cookieId,
                RecoveryCode = GenerateRecoveryCode(),
                DeviceFingerprint = request.DeviceFingerprint,
                TermsAcceptedAt = DateTimeOffset.UtcNow
            };
            db.AnonymousUsers.Add(anonymous);
        }
        else
        {
            anonymous.TermsAcceptedAt ??= DateTimeOffset.UtcNow;
            anonymous.DeviceFingerprint ??= request.DeviceFingerprint;
        }

        await db.SaveChangesAsync(ct);

        if (user is UserContext mutable)
        {
            mutable.AnonymousUserId = anonymous.Id;
            mutable.AnonymousCookieId = anonymous.CookieId;
        }

        http.Items["AnonymousUser"] = anonymous;
        AppendAnonymousCookies(http, anonymous);

        await SystemLeagueService.EnsureSystemLeaguesAsync(db, user, chosenCountry, ct);
        await db.SaveChangesAsync(ct);

        var recoveryToken = tokens.CreateRecoveryToken(anonymous.Id);
        var csrfToken = CsrfMiddleware.IssueToken(http);

        return Results.Ok(new SessionResponse(
            Authenticated: false,
            Anonymous: true,
            TermsAccepted: true,
            RecoveryToken: recoveryToken,
            UserId: null,
            AnonymousUserId: anonymous.Id,
            CsrfToken: csrfToken,
            IsPlatformAdmin: false));
    }

    private static async Task<IResult> RecoverSession(
        SessionRecoverRequest request,
        AppDbContext db,
        SessionTokenService tokens,
        TurnstileService turnstile,
        HttpContext http,
        CancellationToken ct)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            return Results.BadRequest(new { error = "Human verification failed." });
        }

        if (!tokens.TryValidateRecoveryToken(request.RecoveryToken, out var anonymousUserId))
        {
            return Results.BadRequest(new { error = "Invalid or expired recovery key." });
        }

        var anonymous = await db.AnonymousUsers.FindAsync([anonymousUserId], ct);
        if (anonymous is null)
        {
            return Results.NotFound(new { error = "Session not found." });
        }

        anonymous.TermsAcceptedAt ??= DateTimeOffset.UtcNow;

        // Device fingerprint check: if a different device is recovering the key,
        // rotate the cookie so the previous device is automatically logged out.
        var incomingFp = request.DeviceFingerprint;
        if (!string.IsNullOrWhiteSpace(incomingFp))
        {
            if (anonymous.DeviceFingerprint is not null &&
                !string.Equals(anonymous.DeviceFingerprint, incomingFp, StringComparison.OrdinalIgnoreCase))
            {
                // New device — rotate the cookieId to invalidate any old browser session
                anonymous.CookieId = Guid.NewGuid().ToString("N");
            }
            anonymous.DeviceFingerprint = incomingFp;
        }

        await db.SaveChangesAsync(ct);

        http.Response.Cookies.Append(AnonymousUserMiddleware.AnonymousCookieName, anonymous.CookieId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = http.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(365)
        });

        http.Items["AnonymousUser"] = anonymous;
        var csrf = CsrfMiddleware.IssueToken(http);

        return Results.Ok(new SessionResponse(
            Authenticated: false,
            Anonymous: true,
            TermsAccepted: true,
            RecoveryToken: request.RecoveryToken.Trim(),
            UserId: null,
            AnonymousUserId: anonymous.Id,
            CsrfToken: csrf,
            IsPlatformAdmin: false));
    }

    private static string ResolveCookieId(HttpContext context) =>
        AnonymousUserMiddleware.ResolveCookieId(context);

    private static void AppendAnonymousCookies(HttpContext context, AnonymousUser anonymous)
    {
        context.Response.Cookies.Append(AnonymousUserMiddleware.AnonymousCookieName, anonymous.CookieId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = TimeSpan.FromDays(365)
        });
    }

    private static string GenerateRecoveryCode() =>
        Convert.ToHexString(Guid.NewGuid().ToByteArray())[..12].ToUpperInvariant();
}

public record SessionConsentRequest(bool AcceptedTerms, string? TurnstileToken, string? DeviceFingerprint = null, string? CountryCode = null);

public record SessionRecoverRequest(string RecoveryToken, string? TurnstileToken, string? DeviceFingerprint = null);

public record SessionResponse(
    bool Authenticated,
    bool Anonymous,
    bool TermsAccepted,
    string? RecoveryToken,
    Guid? UserId,
    Guid? AnonymousUserId,
    string? CsrfToken,
    bool IsPlatformAdmin);
