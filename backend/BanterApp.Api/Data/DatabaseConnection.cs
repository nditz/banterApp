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

            return raw.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
                ? ToNpgsqlConnectionString(raw.Trim())
                : raw.Trim();
        }

        return null;
    }

    private static IEnumerable<string?> GetCandidates(IConfiguration configuration)
    {
        yield return configuration.GetConnectionString(DefaultConnectionName);
        yield return configuration["Database:DirectUrl"];
        yield return configuration["Database:TransactionUrl"];
    }

    /// <summary>
    /// Converts a postgres:// URI to an Npgsql ADO.NET connection string (required by EF design tools).
    /// </summary>
    public static string ToNpgsqlConnectionString(string uri)
    {
        uri = uri.Trim().Trim('"', '\'');

        if (!uri.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !uri.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return uri;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = parsed.Host,
            Port = parsed.Port > 0 ? parsed.Port : 5432,
            Database = parsed.AbsolutePath.TrimStart('/').Split('?')[0],
            SslMode = SslMode.Require
        };

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            var colon = parsed.UserInfo.IndexOf(':');
            if (colon >= 0)
            {
                builder.Username = Uri.UnescapeDataString(parsed.UserInfo[..colon]);
                builder.Password = Uri.UnescapeDataString(parsed.UserInfo[(colon + 1)..]);
            }
            else
            {
                builder.Username = Uri.UnescapeDataString(parsed.UserInfo);
            }
        }

        // Transaction pooler (port 6543) — avoid prepared statements that break with PgBouncer.
        if (parsed.Port == 6543 || QueryFlag(parsed, "pgbouncer"))
        {
            builder.MaxAutoPrepare = 0;
            builder.NoResetOnClose = true;
        }

        return builder.ConnectionString;
    }

    private static bool QueryFlag(Uri parsed, string key)
    {
        if (string.IsNullOrEmpty(parsed.Query))
        {
            return false;
        }

        var query = parsed.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 &&
                string.Equals(kv[0], key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(kv[1], "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
