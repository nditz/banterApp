using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using BanterApp.Api.Features.Auth;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Features.Admin;

/// <summary>
/// Server-only wrapper around the Supabase Auth Admin API. This is the single place the
/// service-role key is used; it must never be surfaced to a response, a log or the
/// browser.
/// </summary>
public interface ISupabaseAdminClient
{
    bool IsConfigured { get; }

    Task<SupabaseAdminUserPage?> ListUsersAsync(int page, int perPage, CancellationToken ct = default);

    Task<SupabaseAdminUser?> GetUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed class SupabaseAdminClient(
    HttpClient http,
    IOptions<SupabaseOptions> options,
    ILogger<SupabaseAdminClient> logger) : ISupabaseAdminClient
{
    private readonly SupabaseOptions _options = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Url) &&
        !string.IsNullOrWhiteSpace(_options.ServiceRoleKey);

    public async Task<SupabaseAdminUserPage?> ListUsersAsync(
        int page,
        int perPage,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        // Supabase paginates admin user listing from 1.
        var path = $"/auth/v1/admin/users?page={page}&per_page={perPage}";
        var body = await SendAsync(HttpMethod.Get, path, ct);
        if (body is null)
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<SupabaseAdminUserListResponse>(body, JsonOptions);
            if (parsed is null)
            {
                return null;
            }

            return new SupabaseAdminUserPage(parsed.Users ?? [], parsed.Total);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not parse Supabase admin user list response.");
            return null;
        }
    }

    public async Task<SupabaseAdminUser?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var body = await SendAsync(HttpMethod.Get, $"/auth/v1/admin/users/{userId}", ct);
        if (body is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SupabaseAdminUser>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Could not parse Supabase admin user response.");
            return null;
        }
    }

    private async Task<string?> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, $"{_options.Url.TrimEnd('/')}{path}");
        request.Headers.Add("apikey", _options.ServiceRoleKey);
        request.Headers.Add("Authorization", $"Bearer {_options.ServiceRoleKey}");

        try
        {
            var response = await http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Status only. The response body can echo request headers, which would
                // put the service-role key into the logs.
                logger.LogWarning(
                    "Supabase admin request to {Path} failed with {Status}.",
                    path,
                    (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Supabase admin request to {Path} could not be completed.", path);
            return null;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Supabase admin request to {Path} timed out.", path);
            return null;
        }
    }
}

public sealed record SupabaseAdminUserPage(IReadOnlyList<SupabaseAdminUser> Users, int? Total);

public sealed class SupabaseAdminUserListResponse
{
    public List<SupabaseAdminUser>? Users { get; set; }

    public int? Total { get; set; }
}

public sealed class SupabaseAdminUser
{
    public string? Id { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("last_sign_in_at")]
    public DateTimeOffset? LastSignInAt { get; set; }

    [JsonPropertyName("email_confirmed_at")]
    public DateTimeOffset? EmailConfirmedAt { get; set; }

    [JsonPropertyName("banned_until")]
    public DateTimeOffset? BannedUntil { get; set; }

    [JsonPropertyName("app_metadata")]
    public SupabaseAdminAppMetadata? AppMetadata { get; set; }

    [JsonPropertyName("user_metadata")]
    public SupabaseAdminUserMetadata? UserMetadata { get; set; }

    public Guid? ParsedId => Guid.TryParse(Id, out var id) ? id : null;

    /// <summary>
    /// Login methods, e.g. "email" or "google". Never includes provider tokens.
    /// </summary>
    public IReadOnlyList<string> Providers
    {
        get
        {
            if (AppMetadata?.Providers is { Count: > 0 } providers)
            {
                return providers;
            }

            return string.IsNullOrWhiteSpace(AppMetadata?.Provider)
                ? []
                : [AppMetadata.Provider];
        }
    }
}

public sealed class SupabaseAdminAppMetadata
{
    public string? Provider { get; set; }

    public List<string>? Providers { get; set; }
}

public sealed class SupabaseAdminUserMetadata
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}
