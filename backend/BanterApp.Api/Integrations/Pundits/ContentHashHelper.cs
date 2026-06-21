using System.Security.Cryptography;
using System.Text;

namespace BanterApp.Api.Integrations.Pundits;

public static class ContentHashHelper
{
    public static string Compute(string externalId, string url, string title)
    {
        var normalized = $"{externalId.Trim().ToLowerInvariant()}|{url.Trim().ToLowerInvariant()}|{title.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
