using System.Net;
using System.Net.Sockets;

namespace BanterApp.Api.Services;

public interface IOutboundUrlValidator
{
    Task<UrlValidationResult> ValidateAsync(string url, CancellationToken ct = default);
    Task<UrlValidationResult> ValidateRedirectAsync(Uri uri, string originalUrl, CancellationToken ct);
}

public sealed record UrlValidationResult(bool IsAllowed, string? Reason = null);

public sealed class OutboundUrlValidator(
    IConfiguration configuration,
    IApplicationErrorLogger errorLogger) : IOutboundUrlValidator
{
    public const int MaxRedirectsAllowed = 3;

    public async Task<UrlValidationResult> ValidateAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return await BlockAsync(url, "invalid_uri", ct);
        }

        if (uri.Scheme is not "http" and not "https")
        {
            return await BlockAsync(url, "invalid_scheme", ct);
        }

        if (!IsHostAllowed(uri.Host))
        {
            return await BlockAsync(url, "host_not_allowed", ct);
        }

        return await ValidateHostAsync(uri.Host, url, ct);
    }

    public async Task<UrlValidationResult> ValidateRedirectAsync(Uri uri, string originalUrl, CancellationToken ct)
    {
        if (uri.Scheme is not "http" and not "https")
        {
            return await BlockAsync(originalUrl, "redirect_invalid_scheme", ct);
        }

        if (!IsHostAllowed(uri.Host))
        {
            return await BlockAsync(originalUrl, "redirect_host_not_allowed", ct);
        }

        return await ValidateHostAsync(uri.Host, originalUrl, ct);
    }

    private bool IsHostAllowed(string host)
    {
        var allowlist = configuration.GetSection("Security:AllowedFetchDomains").Get<string[]>();
        if (allowlist is null || allowlist.Length == 0)
        {
            return true;
        }

        return allowlist.Any(domain =>
            host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<UrlValidationResult> ValidateHostAsync(string host, string url, CancellationToken ct)
    {
        if (IsBlockedLiteralHost(host))
        {
            return await BlockAsync(url, "blocked_host", ct);
        }

        if (IPAddress.TryParse(host, out var literalIp))
        {
            return IsPrivateOrReserved(literalIp)
                ? await BlockAsync(url, "private_ip", ct)
                : new UrlValidationResult(true);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            if (addresses.Length == 0)
            {
                return await BlockAsync(url, "dns_empty", ct);
            }

            foreach (var address in addresses)
            {
                if (IsPrivateOrReserved(address))
                {
                    return await BlockAsync(url, "private_ip_resolved", ct);
                }
            }
        }
        catch (Exception ex)
        {
            await errorLogger.LogAsync(
                "ssrf",
                $"DNS resolution failed for outbound URL host {host}: {ex.Message}",
                category: "ssrf_blocked",
                ct: ct);
            return new UrlValidationResult(false, "dns_failed");
        }

        return new UrlValidationResult(true);
    }

    private static bool IsBlockedLiteralHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase);

    public static bool IsPrivateOrReserved(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 0xFC || bytes[0] == 0xFD)
            {
                return true;
            }

            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            {
                return true;
            }

            if (bytes.Take(12).SequenceEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF }))
            {
                address = new IPAddress(bytes.Skip(12).Take(4).ToArray());
            }
            else
            {
                return false;
            }
        }

        var ipv4 = address.GetAddressBytes();
        if (ipv4[0] == 10)
        {
            return true;
        }

        if (ipv4[0] == 127)
        {
            return true;
        }

        if (ipv4[0] == 169 && ipv4[1] == 254)
        {
            return true;
        }

        if (ipv4[0] == 172 && ipv4[1] >= 16 && ipv4[1] <= 31)
        {
            return true;
        }

        if (ipv4[0] == 192 && ipv4[1] == 168)
        {
            return true;
        }

        if (ipv4[0] == 0)
        {
            return true;
        }

        return false;
    }

    private async Task<UrlValidationResult> BlockAsync(string url, string reason, CancellationToken ct)
    {
        await errorLogger.LogAsync(
            "ssrf",
            $"Blocked outbound URL ({reason}): {url}",
            category: "ssrf_blocked",
            ct: ct);
        return new UrlValidationResult(false, reason);
    }
}
