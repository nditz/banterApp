using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Services;

namespace BanterApp.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .RequireRateLimiting(RateLimitPolicies.AuthSignup)
            .WithValidation<RegisterRequest>();
        group.MapPost("/login", Login)
            .RequireRateLimiting(RateLimitPolicies.AuthLogin)
            .WithValidation<LoginRequest>();
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        app.MapSessionEndpoints();

        return app;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        SupabaseAuthService auth,
        TurnstileService turnstile,
        IAuthAuditService audit,
        HttpContext http,
        CancellationToken ct)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            await audit.LogAsync("register", false, request.Email, ipAddress: ip, userAgent: ua,
                details: "turnstile_failed", ct: ct);
            return Results.BadRequest(new AuthErrorResponse("Human verification failed."));
        }

        if (IsBlockedEmailDomain(request.Email, http.RequestServices))
        {
            await audit.LogAsync("register", false, request.Email, ipAddress: ip, userAgent: ua,
                details: "blocked_email_domain", ct: ct);
            return Results.BadRequest(new AuthErrorResponse("Registration is not available for this email domain."));
        }

        var (success, _) = await auth.RegisterAsync(request, ct);
        if (success is null)
        {
            await audit.LogAsync("register", false, request.Email, ipAddress: ip, userAgent: ua, ct: ct);
            return Results.BadRequest(new AuthErrorResponse("Registration failed. Check your details and try again."));
        }

        await audit.LogAsync("register", true, request.Email, success.UserId, ip, ua, ct: ct);
        return Results.Ok(success);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        SupabaseAuthService auth,
        TurnstileService turnstile,
        IAuthAuditService audit,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        if (!await turnstile.VerifyAsync(request.TurnstileToken, ip, ct))
        {
            await audit.LogAsync("login", false, request.Email, ipAddress: ip, userAgent: ua,
                details: "turnstile_failed", ct: ct);
            return Results.BadRequest(new AuthErrorResponse("Human verification failed."));
        }

        var (success, _) = await auth.LoginAsync(request, ct);
        if (success is null)
        {
            await audit.LogAsync("login", false, request.Email, ipAddress: ip, userAgent: ua, ct: ct);
            return Results.Unauthorized();
        }

        var user = await db.Users.FindAsync([success.UserId], ct);
        if (user?.AccountStatus is AccountStatus.Suspended or AccountStatus.Banned)
        {
            await audit.LogAsync("login", false, request.Email, success.UserId, ip, ua,
                details: $"account_{user.AccountStatus}", ct: ct);
            return Results.Unauthorized();
        }

        await audit.LogAsync("login", true, request.Email, success.UserId, ip, ua, ct: ct);
        return Results.Ok(success);
    }

    private static IResult GetCurrentUser(IUserContext user) =>
        user.IsAuthenticated
            ? Results.Ok(new { userId = user.UserId })
            : Results.Unauthorized();

    private static bool IsBlockedEmailDomain(string email, IServiceProvider services)
    {
        var at = email.LastIndexOf('@');
        if (at < 0)
        {
            return false;
        }

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();
        var blocked = services.GetRequiredService<IConfiguration>()
            .GetSection("Security:BlockedEmailDomains")
            .Get<string[]>() ?? [];

        return blocked.Any(d => domain.Equals(d, StringComparison.OrdinalIgnoreCase));
    }
}
