using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BanterApp.Api.Common;

public static partial class ErrorSanitizer
{
    private const int MaxFieldLength = 8000;
    private const int MaxBlobLength = 500;

    private static readonly string[] SensitiveKeyFragments =
    [
        "key", "token", "secret", "password", "authorization", "credential", "apikey",
        "cookie", "session", "csrf", "bearer", "refresh", "private"
    ];

    public static string SanitizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json ?? string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return RedactSensitiveText(Truncate(json));
            }

            SanitizeNode(node);
            return Truncate(node.ToJsonString());
        }
        catch (JsonException)
        {
            return RedactSensitiveText(Truncate(json));
        }
    }

    public static object? SanitizeObject(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<object>(SanitizeJson(json));
    }

    public static string RedactSensitiveText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var result = text;
        result = AuthorizationHeaderPattern().Replace(result, "Authorization: [REDACTED]");
        result = CookiePattern().Replace(result, "Cookie: [REDACTED]");
        result = DatabaseUrlPattern().Replace(result, "[REDACTED_DB_URL]");
        result = JwtPattern().Replace(result, "[REDACTED_TOKEN]");
        result = ApiKeyPattern().Replace(result, "[REDACTED]");
        result = EmailPattern().Replace(result, "[REDACTED_EMAIL]");
        result = PhonePattern().Replace(result, "[REDACTED_PHONE]");
        return Truncate(result);
    }

    public static string? SanitizeMessage(string? message) =>
        string.IsNullOrWhiteSpace(message) ? message : RedactSensitiveText(message);

    public static string? SanitizeStackTrace(string? stackTrace) =>
        string.IsNullOrWhiteSpace(stackTrace) ? stackTrace : RedactSensitiveText(Truncate(stackTrace));

    private static string Truncate(string value)
    {
        if (value.Length <= MaxFieldLength)
        {
            return value;
        }

        return value[..MaxFieldLength] + "…[TRUNCATED]";
    }

    private static void SanitizeNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (property.Key is null)
                    {
                        continue;
                    }

                    if (IsSensitiveKey(property.Key))
                    {
                        obj[property.Key] = "[REDACTED]";
                        continue;
                    }

                    if (property.Value is JsonValue jsonValue &&
                        jsonValue.TryGetValue<string>(out var strValue) &&
                        strValue.Length > MaxBlobLength)
                    {
                        obj[property.Key] = strValue[..MaxBlobLength] + "…[TRUNCATED]";
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        SanitizeNode(property.Value);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        SanitizeNode(item);
                    }
                }

                break;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return SensitiveKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"(?i)authorization\s*:\s*[^\r\n]+", RegexOptions.Compiled)]
    private static partial Regex AuthorizationHeaderPattern();

    [GeneratedRegex(@"(?i)cookie\s*:\s*[^\r\n]+", RegexOptions.Compiled)]
    private static partial Regex CookiePattern();

    [GeneratedRegex(@"(?i)(postgres(?:ql)?|mysql|mongodb(\+srv)?|redis)://[^\s""']+", RegexOptions.Compiled)]
    private static partial Regex DatabaseUrlPattern();

    [GeneratedRegex(@"(?i)eyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+", RegexOptions.Compiled)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?i)(api[_-]?key|token|secret|password|bearer)\s*[:=]\s*[^\s,}""']+", RegexOptions.Compiled)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\+?\d[\d\s().-]{7,}\d\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();
}
