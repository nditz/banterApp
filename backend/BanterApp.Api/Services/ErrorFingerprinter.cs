using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BanterApp.Api.Services;

public static partial class ErrorFingerprinter
{
    public static string Compute(
        string environment,
        string source,
        string errorCode,
        string? errorType,
        string? route,
        string? jobKey,
        string? provider,
        string message,
        string? stackTrace)
    {
        var normalizedMessage = NormalizeMessage(message);
        var topFrame = ExtractTopStackFrame(stackTrace);

        var payload = string.Join('|',
            environment.Trim().ToLowerInvariant(),
            source.Trim().ToLowerInvariant(),
            errorCode.Trim().ToUpperInvariant(),
            (errorType ?? string.Empty).Trim().ToLowerInvariant(),
            (route ?? string.Empty).Trim().ToLowerInvariant(),
            (jobKey ?? string.Empty).Trim().ToLowerInvariant(),
            (provider ?? string.Empty).Trim().ToLowerInvariant(),
            normalizedMessage,
            topFrame);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var normalized = message.ToLowerInvariant();
        normalized = GuidPattern().Replace(normalized, "{guid}");
        normalized = NumberPattern().Replace(normalized, "{n}");
        normalized = TimestampPattern().Replace(normalized, "{ts}");
        return normalized.Trim();
    }

    private static string ExtractTopStackFrame(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return string.Empty;
        }

        foreach (var line in stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.Contains("BanterApp", StringComparison.Ordinal) && line.StartsWith("at ", StringComparison.Ordinal))
            {
                return line;
            }
        }

        return stackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;
    }

    [GeneratedRegex(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GuidPattern();

    [GeneratedRegex(@"\b\d+\b", RegexOptions.Compiled)]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}[tT\s]\d{2}:\d{2}:\d{2}", RegexOptions.Compiled)]
    private static partial Regex TimestampPattern();
}
