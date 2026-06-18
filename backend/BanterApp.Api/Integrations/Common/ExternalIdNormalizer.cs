using System.Security.Cryptography;
using System.Text;
using BanterApp.Api.Common;

namespace BanterApp.Api.Integrations.Common;

public static class ExternalIdNormalizer
{
    public static string Normalize(string value, int maxLength = StringLimits.ExternalId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trimmed)))[..32];
        return $"hash:{hash}";
    }
}
