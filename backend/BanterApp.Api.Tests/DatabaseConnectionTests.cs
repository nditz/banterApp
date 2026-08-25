using BanterApp.Api.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace BanterApp.Api.Tests;

public class DatabaseConnectionTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Resolve_PrefersDatabaseUrlConfigKey()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "postgresql://user:pass@db.example.com:5432/postgres",
            ["ConnectionStrings:DefaultConnection"] = "postgresql://other:other@other.example.com:5432/postgres",
        });

        var resolved = DatabaseConnection.Resolve(config);

        Assert.NotNull(resolved);
        var parsed = new NpgsqlConnectionStringBuilder(resolved);
        Assert.Equal("db.example.com", parsed.Host);
        Assert.Equal("user", parsed.Username);
    }

    [Fact]
    public void Resolve_FallsBackToDefaultConnection()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "postgresql://fallback:secret@fallback.example.com:5432/postgres",
        });

        var resolved = DatabaseConnection.Resolve(config);

        Assert.NotNull(resolved);
        var parsed = new NpgsqlConnectionStringBuilder(resolved);
        Assert.Equal("fallback.example.com", parsed.Host);
        Assert.Equal("fallback", parsed.Username);
    }

    [Fact]
    public void ToNpgsqlConnectionString_ConvertsSupabaseUri()
    {
        const string uri =
            "postgresql://postgres.projectref:p%40ss%2Fword@aws-0-eu-west-1.pooler.supabase.com:5432/postgres";

        var result = DatabaseConnection.ToNpgsqlConnectionString(uri);

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("aws-0-eu-west-1.pooler.supabase.com", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("postgres", parsed.Database);
        Assert.Equal("postgres.projectref", parsed.Username);
        Assert.Equal("p@ss/word", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_TransactionPooler_DisablesAutoPrepare()
    {
        const string uri =
            "postgresql://postgres.projectref:secret@aws-0-eu-west-1.pooler.supabase.com:6543/postgres?pgbouncer=true";

        var result = DatabaseConnection.ToNpgsqlConnectionString(uri);

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(0, parsed.MaxAutoPrepare);
        Assert.True(parsed.NoResetOnClose);
    }

    [Fact]
    public void ToNpgsqlConnectionString_HandlesUnencodedSpecialCharsInPassword()
    {
        // Real-world case: password copied straight from the Supabase dashboard
        // containing unencoded special characters. Uri.TryCreate cannot handle this.
        const string uri =
            "postgresql://postgres.projectref:p@ss/w0rd!@aws-0-eu-west-1.pooler.supabase.com:5432/postgres";

        var result = DatabaseConnection.ToNpgsqlConnectionString(uri);

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("aws-0-eu-west-1.pooler.supabase.com", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("postgres", parsed.Database);
        Assert.Equal("postgres.projectref", parsed.Username);
        Assert.Equal("p@ss/w0rd!", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_HandlesSpaceInPassword()
    {
        // A space is illegal in a URI, so Uri.TryCreate cannot parse this at all.
        const string uri =
            "postgresql://postgres.SomeRef:some passwad@aws-0-eu-west-1.pooler.supabase.com:6543/postgres";

        var result = DatabaseConnection.ToNpgsqlConnectionString(uri);

        var parsed = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("aws-0-eu-west-1.pooler.supabase.com", parsed.Host);
        Assert.Equal(6543, parsed.Port);
        Assert.Equal("postgres.SomeRef", parsed.Username);
        Assert.Equal("some passwad", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
        // Port 6543 = transaction pooler → prepared statements disabled.
        Assert.Equal(0, parsed.MaxAutoPrepare);
    }

    [Fact]
    public void Resolve_TrimsWhitespaceBeforeDetectingUri()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "\n  postgresql://user:pass@db.example.com:5432/postgres  \n",
        });

        var resolved = DatabaseConnection.Resolve(config);

        Assert.NotNull(resolved);
        var parsed = new NpgsqlConnectionStringBuilder(resolved);
        Assert.Equal("db.example.com", parsed.Host);
        Assert.Equal("user", parsed.Username);
    }

    [Theory]
    [InlineData("postgresql://postgres:pass@db.mpromkefxwanjqbzvown.supabase.co:5432/postgres", true)]
    [InlineData("Host=db.mpromkefxwanjqbzvown.supabase.co;Port=5432;Database=postgres", true)]
    [InlineData("postgresql://postgres.ref:pass@aws-0-eu-west-1.pooler.supabase.com:6543/postgres", false)]
    [InlineData("Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres", false)]
    [InlineData("", false)]
    public void IsDirectSupabaseConnection_DetectsIpv6OnlyDirectHost(string connectionString, bool expected)
    {
        Assert.Equal(expected, DatabaseConnection.IsDirectSupabaseConnection(connectionString));
    }

    [Fact]
    public void ToNpgsqlConnectionString_LocalUri_DoesNotForceTls()
    {
        // A stock local Postgres serves plaintext. Forcing Require here produced an opaque
        // TLS error and pushed local development onto the ADO.NET form unnecessarily.
        const string uri = "postgresql://postgres:postgres@localhost:5433/banterapp";

        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsqlConnectionString(uri));

        Assert.Equal("localhost", parsed.Host);
        Assert.Equal(5433, parsed.Port);
        Assert.Equal("banterapp", parsed.Database);
        Assert.Equal(SslMode.Prefer, parsed.SslMode);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void ToNpgsqlConnectionString_LoopbackAddresses_UsePrefer(string host)
    {
        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsqlConnectionString(
                $"postgresql://postgres:postgres@{host}:54322/postgres"));

        Assert.Equal(SslMode.Prefer, parsed.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_RemoteHostWithoutSslMode_StillRequiresTls()
    {
        // The guard that matters: a managed database must never silently downgrade.
        const string uri =
            "postgresql://postgres.ref:secret@aws-0-eu-west-1.pooler.supabase.com:5432/postgres";

        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsqlConnectionString(uri));

        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Theory]
    [InlineData("disable", SslMode.Disable)]
    [InlineData("prefer", SslMode.Prefer)]
    [InlineData("require", SslMode.Require)]
    [InlineData("verify-full", SslMode.VerifyFull)]
    [InlineData("VERIFY-CA", SslMode.VerifyCA)]
    public void ToNpgsqlConnectionString_HonorsExplicitSslMode(string sslMode, SslMode expected)
    {
        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsqlConnectionString(
                $"postgresql://postgres:postgres@localhost:5433/banterapp?sslmode={sslMode}"));

        Assert.Equal(expected, parsed.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_UnrecognizedSslMode_FallsBackToHostDefault()
    {
        var parsed = new NpgsqlConnectionStringBuilder(
            DatabaseConnection.ToNpgsqlConnectionString(
                "postgresql://postgres.ref:secret@aws-0-eu-west-1.pooler.supabase.com:5432/postgres?sslmode=nonsense"));

        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void ToNpgsqlConnectionString_NonUri_ReturnsAsIs()
    {
        const string ado = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=secret";

        var result = DatabaseConnection.ToNpgsqlConnectionString(ado);

        Assert.Equal(ado, result);
    }
}
