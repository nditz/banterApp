using System.Text;
using System.Text.RegularExpressions;

namespace BanterApp.Api.Common;

/// <summary>Basic PG profanity check for user-supplied league names.</summary>
public static partial class ProfanityFilter
{
    private static readonly string[] BlockedTerms =
    [
        "asshole", "bastard", "bitch", "bollocks", "bullshit", "cock", "crap", "cunt",
        "damn", "dick", "fuck", "fucking", "motherfucker", "piss", "pussy", "shit",
        "slut", "twat", "wanker", "whore",
    ];

    public static bool ContainsProfanity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Normalize(value);
        foreach (var term in BlockedTerms)
        {
            if (normalized.Contains(term, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lower.Length);

        foreach (var ch in lower)
        {
            builder.Append(ch switch
            {
                '0' => 'o',
                '1' => 'i',
                '3' => 'e',
                '4' => 'a',
                '5' => 's',
                '7' => 't',
                '@' => 'a',
                '$' => 's',
                _ => ch,
            });
        }

        return NonAlphaRegex().Replace(builder.ToString(), " ");
    }

    [GeneratedRegex(@"[^a-z0-9\s]")]
    private static partial Regex NonAlphaRegex();
}
