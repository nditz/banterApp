using System.Security.Cryptography;
using System.Text;

namespace BanterApp.Api.Services;

public sealed class SessionTokenService(IConfiguration configuration)
{
    private const string Prefix = "banter.v1";

    public string CreateRecoveryToken(Guid anonymousUserId)
    {
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{anonymousUserId:N}:{issuedAt}";
        var signature = Sign(payload);
        return $"{Prefix}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{signature}";
    }

    public bool TryValidateRecoveryToken(string token, out Guid anonymousUserId)
    {
        anonymousUserId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Trim().Split('.');
        if (parts.Length != 3 || parts[0] != Prefix)
        {
            return false;
        }

        byte[] payloadBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[1]);
        }
        catch
        {
            return false;
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(payload)),
                Encoding.UTF8.GetBytes(parts[2])))
        {
            return false;
        }

        var segments = payload.Split(':');
        if (segments.Length != 2 || !Guid.TryParseExact(segments[0], "N", out anonymousUserId))
        {
            return false;
        }

        if (!long.TryParse(segments[1], out var issuedAt))
        {
            return false;
        }

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAt);
        return age <= TimeSpan.FromDays(365);
    }

    private string Sign(string payload)
    {
        var key = ResolveSecret();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private string ResolveSecret()
    {
        var secret = configuration["Security:SessionSecret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = "banter-dev-session-secret-change-in-production";
        }

        return secret;
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
