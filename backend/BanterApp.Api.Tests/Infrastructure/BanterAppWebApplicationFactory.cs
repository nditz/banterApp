using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BanterApp.Api.Tests.Infrastructure;

public static class TestUsers
{
    public const string AdminEmail = "admin@test.com";
    public static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User-Id", out var userIdHeader) ||
            !Guid.TryParse(userIdHeader.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Request.Headers.TryGetValue("X-Test-Email", out var emailHeader);
        var email = emailHeader.ToString();

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
            claims.Add(new Claim("email", email));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class BanterAppWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundJobs:Enabled"] = "false",
                ["Admin:AllowedEmails:0"] = TestUsers.AdminEmail,
                ["Admin:ExposeErrorDetail"] = "true",
                ["ConnectionStrings:DefaultConnection"] = "",
                ["Database:DirectUrl"] = "",
                ["Database:TransactionUrl"] = "",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string email, Guid userId)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        return client;
    }

    public HttpClient CreateAdminClient() =>
        CreateAuthenticatedClient(TestUsers.AdminEmail, TestUsers.AdminId);

    public HttpClient CreateNonAdminClient() =>
        CreateAuthenticatedClient("user@test.com", TestUsers.UserId);
}

public static class CsrfTestHelper
{
    public static async Task ApplyCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/session");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SessionCsrfResponse>();
        if (payload?.CsrfToken is not null)
        {
            client.DefaultRequestHeaders.Remove("X-CSRF-Token");
            client.DefaultRequestHeaders.Add("X-CSRF-Token", payload.CsrfToken);
        }

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var csrfCookie = cookies
                .FirstOrDefault(c => c.StartsWith("banter_csrf=", StringComparison.OrdinalIgnoreCase));
            if (csrfCookie is not null)
            {
                var value = csrfCookie.Split(';')[0]["banter_csrf=".Length..];
                client.DefaultRequestHeaders.Remove("Cookie");
                client.DefaultRequestHeaders.Add("Cookie", $"banter_csrf={value}");
            }
        }
    }

    private sealed record SessionCsrfResponse(string? CsrfToken);
}
