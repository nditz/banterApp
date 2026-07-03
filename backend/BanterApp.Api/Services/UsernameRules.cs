using System.Text.RegularExpressions;

namespace BanterApp.Api.Services;

public static class UsernameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 20;

    private static readonly Regex ValidPattern = new("^[a-zA-Z0-9]+$", RegexOptions.Compiled);

    public static bool IsValidFormat(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        var trimmed = username.Trim();
        return trimmed.Length is >= MinLength and <= MaxLength && ValidPattern.IsMatch(trimmed);
    }

    public static string? Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = new string(raw.Where(c => char.IsAsciiLetterOrDigit(c)).ToArray());
        if (cleaned.Length < MinLength)
        {
            return null;
        }

        if (cleaned.Length > MaxLength)
        {
            cleaned = cleaned[..MaxLength];
        }

        return cleaned;
    }

    public static string NormalizeKey(string username) =>
        username.Trim().ToLowerInvariant();
}
