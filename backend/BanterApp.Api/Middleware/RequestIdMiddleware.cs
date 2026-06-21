using System.Text.RegularExpressions;

namespace BanterApp.Api.Middleware;

public sealed partial class RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
{
    public const string HeaderName = "X-Request-Id";
    public const string ItemKey = "RequestId";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var requestId = IsValidIncomingRequestId(incoming)
            ? incoming
            : $"req_{Guid.NewGuid():N}";

        context.Items[ItemKey] = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = requestId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await next(context);
        }
    }

    private static bool IsValidIncomingRequestId(string value) =>
        !string.IsNullOrWhiteSpace(value) && IncomingRequestIdPattern().IsMatch(value);

    [GeneratedRegex(@"^[a-zA-Z0-9_-]{8,64}$", RegexOptions.Compiled)]
    private static partial Regex IncomingRequestIdPattern();
}
