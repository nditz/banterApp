using Npgsql;

namespace BanterApp.Api.Data;

/// <summary>
/// Resolves PostgreSQL connection strings from ASP.NET Core configuration.
/// Local: <c>appsettings.Development.json</c> → <c>ConnectionStrings:DefaultConnection</c> (gitignored).
/// Deploy: set the same key via host env vars or GitHub Actions secrets
/// (<c>ConnectionStrings__DefaultConnection</c>).
/// </summary>
public static class DatabaseConnection
{
    public const string DefaultConnectionName = "DefaultConnection";

    public static string? Resolve(IConfiguration configuration)
    {
        foreach (var raw in GetCandidates(configuration))
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            // Trim first so leading/trailing whitespace (common in copy-pasted env vars)
            // doesn't defeat the postgres:// detection below.
            var value = raw.Trim().Trim('"', '\'');

            return value.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
                ? ToNpgsqlConnectionString(value)
                : value;
        }

        return null;
    }

    /// <summary>
    /// True when the connection string targets Supabase's <c>db.&lt;ref&gt;.supabase.co</c>
    /// direct endpoint, which is IPv6-only and therefore unreachable from IPv4-only
    /// hosts such as Render. Detected in both URI and ADO.NET key/value forms.
    /// </summary>
    public static bool IsDirectSupabaseConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var isDirectHost =
            connectionString.Contains("@db.", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Host=db.", StringComparison.OrdinalIgnoreCase);

        return isDirectHost &&
            connectionString.Contains(".supabase.co", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string?> GetCandidates(IConfiguration configuration)
    {
        // DATABASE_URL is read through IConfiguration so the default environment-variable
        // source (and host overrides like Render) are honored, while tests can still
        // neutralize it via in-memory configuration.
        yield return configuration["DATABASE_URL"];
        yield return configuration.GetConnectionString(DefaultConnectionName);
        yield return configuration["Database:DirectUrl"];
        yield return configuration["Database:TransactionUrl"];
    }

    /// <summary>
    /// Converts a postgres:// URI to an Npgsql ADO.NET connection string.
    /// <para>
    /// Parsed manually instead of via <see cref="Uri"/> because real Supabase passwords
    /// frequently contain characters (<c>@ / ? # : %</c>) that are not URL-encoded when
    /// copied from the dashboard. <see cref="Uri"/> either throws or silently misparses
    /// those, which previously caused Npgsql to receive the raw URI and fail with
    /// "Format of the initialization string does not conform to specification".
    /// </para>
    /// </summary>
    public static string ToNpgsqlConnectionString(string uri)
    {
        uri = uri.Trim().Trim('"', '\'');

        var schemeIndex = uri.IndexOf("://", StringComparison.Ordinal);
        var isPostgresUri =
            uri.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

        if (!isPostgresUri || schemeIndex < 0)
        {
            // Already an ADO.NET key/value connection string (or unrecognized) — pass through.
            return uri;
        }

        var rest = uri[(schemeIndex + 3)..];

        // Split off the query string (e.g. ?pgbouncer=true&sslmode=require).
        string query = string.Empty;
        var queryIndex = rest.IndexOf('?');
        if (queryIndex >= 0)
        {
            query = rest[(queryIndex + 1)..];
            rest = rest[..queryIndex];
        }

        // Separate userinfo from host. Use the LAST '@' so unencoded '@' in the
        // password doesn't get mistaken for the userinfo/host boundary.
        string userInfo = string.Empty;
        var hostSection = rest;
        var atIndex = rest.LastIndexOf('@');
        if (atIndex >= 0)
        {
            userInfo = rest[..atIndex];
            hostSection = rest[(atIndex + 1)..];
        }

        // hostSection = host[:port][/database]
        string database = "postgres";
        var slashIndex = hostSection.IndexOf('/');
        if (slashIndex >= 0)
        {
            database = hostSection[(slashIndex + 1)..];
            hostSection = hostSection[..slashIndex];
        }

        string host = hostSection;
        var port = 5432;
        var colonIndex = hostSection.LastIndexOf(':');
        if (colonIndex >= 0)
        {
            host = hostSection[..colonIndex];
            if (int.TryParse(hostSection[(colonIndex + 1)..], out var parsedPort) && parsedPort > 0)
            {
                port = parsedPort;
            }
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = string.IsNullOrEmpty(database) ? "postgres" : database,
            SslMode = ResolveSslMode(query, host)
        };

        if (userInfo.Length > 0)
        {
            // First ':' separates username and password; passwords may contain ':'.
            var credColon = userInfo.IndexOf(':');
            if (credColon >= 0)
            {
                builder.Username = Uri.UnescapeDataString(userInfo[..credColon]);
                builder.Password = Uri.UnescapeDataString(userInfo[(credColon + 1)..]);
            }
            else
            {
                builder.Username = Uri.UnescapeDataString(userInfo);
            }
        }

        // Transaction pooler (port 6543) — avoid prepared statements that break with PgBouncer.
        if (port == 6543 || QueryFlag(query, "pgbouncer"))
        {
            builder.MaxAutoPrepare = 0;
            builder.NoResetOnClose = true;
        }

        return builder.ConnectionString;
    }

    /// <summary>
    /// Honors an explicit libpq-style <c>sslmode</c> query parameter so the same URI form
    /// works against Supabase and a local server.
    /// <para>
    /// Without one, a loopback host gets <see cref="SslMode.Prefer"/> — a stock local
    /// Postgres serves plaintext, and <see cref="SslMode.Require"/> would fail with an
    /// opaque TLS error — while every remote host keeps <see cref="SslMode.Require"/> so a
    /// managed database can never silently downgrade to plaintext.
    /// </para>
    /// </summary>
    private static SslMode ResolveSslMode(string query, string host)
    {
        var requested = QueryValue(query, "sslmode");
        if (!string.IsNullOrWhiteSpace(requested) &&
            // libpq spells the verify modes with a hyphen; the enum does not.
            Enum.TryParse<SslMode>(requested.Replace("-", string.Empty), ignoreCase: true, out var parsed) &&
            Enum.IsDefined(parsed))
        {
            return parsed;
        }

        return IsLoopbackHost(host) ? SslMode.Prefer : SslMode.Require;
    }

    private static bool IsLoopbackHost(string host)
    {
        var value = host.Trim().Trim('[', ']');

        return value.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("::1", StringComparison.Ordinal) ||
               value.StartsWith("127.", StringComparison.Ordinal);
    }

    private static bool QueryFlag(string query, string key) =>
        string.Equals(QueryValue(query, key), "true", StringComparison.OrdinalIgnoreCase);

    private static string? QueryValue(string query, string key)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1]);
            }
        }

        return null;
    }
}
