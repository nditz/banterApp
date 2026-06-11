using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .WithValidation<RegisterRequest>();
        group.MapPost("/login", Login)
            .WithValidation<LoginRequest>();
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        SupabaseAuthService auth,
        CancellationToken ct)
    {
        var (success, error) = await auth.RegisterAsync(request, ct);
        if (success is null)
        {
            return Results.BadRequest(new AuthErrorResponse(error ?? "Registration failed."));
        }

        return Results.Ok(success);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        SupabaseAuthService auth,
        CancellationToken ct)
    {
        var (success, error) = await auth.LoginAsync(request, ct);
        if (success is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(success);
    }

    private static IResult GetCurrentUser(IUserContext user) =>
        user.IsAuthenticated
            ? Results.Ok(new { userId = user.UserId })
            : Results.Unauthorized();
}
