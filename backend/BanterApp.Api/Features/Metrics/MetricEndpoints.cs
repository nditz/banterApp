using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Metrics;

public sealed record RecordMetricRequest(string Key);

public static class MetricEndpoints
{
    public static IEndpointRouteBuilder MapMetricEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/metrics/event", RecordEvent)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.PublicReactions)
            .WithTags("Metrics");

        return app;
    }

    /// <summary>
    /// Records one product funnel counter. Only allowlisted keys are accepted, and nothing
    /// about the caller is stored, so this cannot become a per-user tracking channel.
    /// </summary>
    private static async Task<IResult> RecordEvent(
        RecordMetricRequest request,
        ProductMetricService metrics,
        CancellationToken ct)
    {
        if (!ProductMetrics.IsAllowed(request.Key))
        {
            return Results.BadRequest(new { error = "Unknown metric key." });
        }

        await metrics.RecordAsync(request.Key, ct: ct);
        return Results.NoContent();
    }
}
