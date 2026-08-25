using BanterApp.Api.Common;

namespace BanterApp.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapPost("/events", IngestEvents)
            .RequireRateLimiting(RateLimitPolicies.AnalyticsIngest);
    }

    private static async Task<IResult> IngestEvents(
        AnalyticsIngestRequest request,
        IAnalyticsIngestService ingest,
        IUserContext user,
        HttpContext http,
        CancellationToken ct)
    {
        var events = request.Events ?? [];

        if (events.Count > AnalyticsEventCatalog.MaxBatchSize)
        {
            return Results.BadRequest(new
            {
                error = $"A batch may contain at most {AnalyticsEventCatalog.MaxBatchSize} events."
            });
        }

        // An uncatalogued name is a client bug worth surfacing, unlike the silent drops
        // that happen for consent or unknown properties.
        var unknown = events
            .Select(e => e.EventName)
            .Where(name => !AnalyticsEventCatalog.IsKnown(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (unknown.Count > 0)
        {
            return Results.BadRequest(new
            {
                error = "Unknown analytics event name.",
                unknownEvents = unknown
            });
        }

        var result = await ingest.IngestAsync(events, user, http, ct);

        // Always accepted. Analytics must never fail a product flow, so a dropped batch
        // is reported through the counts rather than a status code.
        return Results.Accepted(value: new
        {
            accepted = result.Accepted,
            dropped = result.Dropped,
            reason = result.Reason
        });
    }
}

public sealed record AnalyticsIngestRequest(List<AnalyticsEventInput>? Events);
