namespace BanterApp.Api.Features.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? TurnstileToken = null);

public sealed record LoginRequest(
    string Email,
    string Password,
    string? TurnstileToken = null);

public sealed record AuthResponse(
    string AccessToken,
    string? RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName,
    string TokenType = "Bearer");

public sealed record AuthErrorResponse(string Error, string? Details = null);
