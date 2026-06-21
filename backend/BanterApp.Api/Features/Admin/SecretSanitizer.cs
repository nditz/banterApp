namespace BanterApp.Api.Features.Admin;

public static class SecretSanitizer
{
    public static string SanitizeJson(string? json) => Common.ErrorSanitizer.SanitizeJson(json);

    public static object? SanitizeObject(object? value) => Common.ErrorSanitizer.SanitizeObject(value);

    public static string RedactSensitiveText(string text) => Common.ErrorSanitizer.RedactSensitiveText(text);
}
