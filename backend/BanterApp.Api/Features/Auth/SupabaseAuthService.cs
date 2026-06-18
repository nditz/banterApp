using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Auth;

public sealed class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
}

public sealed class SupabaseAuthService(
    HttpClient http,
    IOptions<SupabaseOptions> options,
    AppDbContext db,
    ILogger<SupabaseAuthService> logger)
{
    private readonly SupabaseOptions _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Url) &&
        !string.IsNullOrWhiteSpace(_options.AnonKey);

    public async Task<(AuthResponse? Success, string? Error)> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return (null, GetConfigurationMessage());
        }

        var payload = new
        {
            email = request.Email,
            password = request.Password,
            data = new { display_name = request.Email }
        };

        using var message = CreateRequest(HttpMethod.Post, "/auth/v1/signup", payload);
        var response = await http.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Supabase register failed: {Status} {Body}", response.StatusCode, body);
            return (null, ParseSupabaseError(body) ?? "Registration failed.");
        }

        var session = JsonSerializer.Deserialize<SupabaseSession>(body, JsonOptions);
        if (session?.AccessToken is null || session.User?.Id is null)
        {
            return (null, "Registration succeeded but no session returned. Check email confirmation settings.");
        }

        await UpsertUserAsync(session.User, request.Email, ct);
        return (MapSession(session, request.Email), null);
    }

    public async Task<(AuthResponse? Success, string? Error)> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            return (null, GetConfigurationMessage());
        }

        var payload = new { email = request.Email, password = request.Password };
        using var message = CreateRequest(HttpMethod.Post, "/auth/v1/token?grant_type=password", payload);
        var response = await http.SendAsync(message, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Supabase login failed: {Status}", response.StatusCode);
            return (null, ParseSupabaseError(body) ?? "Invalid email or password.");
        }

        var session = JsonSerializer.Deserialize<SupabaseSession>(body, JsonOptions);
        if (session?.AccessToken is null || session.User?.Id is null)
        {
            return (null, "Login failed.");
        }

        var displayName = session.User.Email ?? request.Email;
        await UpsertUserAsync(session.User, displayName, ct);
        return (MapSession(session, displayName), null);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object payload)
    {
        var request = new HttpRequestMessage(method, $"{_options.Url.TrimEnd('/')}{path}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("apikey", _options.AnonKey);
        return request;
    }

    private async Task UpsertUserAsync(SupabaseUser supabaseUser, string displayName, CancellationToken ct)
    {
        if (!Guid.TryParse(supabaseUser.Id, out var userId))
        {
            return;
        }

        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
        {
            db.Users.Add(new User
            {
                Id = userId,
                Email = supabaseUser.Email ?? string.Empty,
                DisplayName = displayName
            });
        }
        else
        {
            user.Email = supabaseUser.Email ?? user.Email;
            user.DisplayName = displayName;
        }

        await db.SaveChangesAsync(ct);
    }

    private static AuthResponse MapSession(SupabaseSession session, string displayName) =>
        new(
            session.AccessToken!,
            session.RefreshToken,
            Guid.Parse(session.User!.Id!),
            session.User.Email ?? string.Empty,
            displayName);

    private static string? ParseSupabaseError(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error_description", out var desc))
            {
                return desc.GetString();
            }

            if (doc.RootElement.TryGetProperty("msg", out var msg))
            {
                return msg.GetString();
            }

            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static string GetConfigurationMessage() =>
        "Supabase is not configured. Set SUPABASE_URL and NEXT_PUBLIC_SUPABASE_ANON_KEY (or Supabase:Url / Supabase:AnonKey). " +
        "Alternatively, obtain a JWT from Supabase Auth client-side and send Authorization: Bearer <token>.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class SupabaseSession
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        public SupabaseUser? User { get; set; }
    }

    private sealed class SupabaseUser
    {
        public string? Id { get; set; }
        public string? Email { get; set; }

        [JsonPropertyName("user_metadata")]
        public SupabaseUserMetadata? UserMetadata { get; set; }
    }

    private sealed class SupabaseUserMetadata
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }
}
