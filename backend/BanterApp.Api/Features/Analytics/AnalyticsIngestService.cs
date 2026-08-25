using System.Text.Json;
using System.Text.Json.Nodes;
using BanterApp.Api.Common;
using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using BanterApp.Api.Features.Privacy;

namespace BanterApp.Api.Features.Analytics;

public interface IAnalyticsIngestService
{
    Task<AnalyticsIngestResult> IngestAsync(
        IReadOnlyList<AnalyticsEventInput> events,
        IUserContext user,
        HttpContext http,
        CancellationToken ct = default);
}

public sealed class AnalyticsIngestService(
    AppDbContext db,
    IConsentService consent,
    IHostEnvironment environment,
    ILogger<AnalyticsIngestService> logger) : IAnalyticsIngestService
{
    public async Task<AnalyticsIngestResult> IngestAsync(
        IReadOnlyList<AnalyticsEventInput> events,
        IUserContext user,
        HttpContext http,
        CancellationToken ct = default)
    {
        if (events.Count == 0)
        {
            return new AnalyticsIngestResult(0, 0, "empty");
        }

        // A caller with no identity has no consent record, so it can never be treated
        // as having opted in.
        if (!await consent.IsAnalyticsAllowedAsync(user, ct))
        {
            return new AnalyticsIngestResult(0, events.Count, "no_consent");
        }

        var now = DateTimeOffset.UtcNow;
        var countryCode = ResolveCountryCode(http);
        var accepted = 0;
        var dropped = 0;

        foreach (var input in events)
        {
            var definition = AnalyticsEventCatalog.Find(input.EventName);
            if (definition is null)
            {
                dropped++;
                continue;
            }

            db.AnalyticsEvents.Add(new AnalyticsEvent
            {
                Id = Guid.NewGuid(),
                EventName = definition.Name,
                Feature = definition.Feature,
                // Server-assigned. A client-supplied timestamp would be untrustworthy
                // and is another value we would have to validate.
                OccurredAt = now,
                UserId = user.UserId,
                AnonymousSessionId = user.UserId is null ? user.AnonymousUserId : null,
                PropertiesJson = BuildProperties(definition, input.Properties),
                AppVersion = Truncate(input.AppVersion, 32),
                Environment = environment.EnvironmentName,
                CountryCode = countryCode
            });

            accepted++;
        }

        if (accepted == 0)
        {
            return new AnalyticsIngestResult(0, dropped, "no_valid_events");
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Analytics must never surface a failure into a product flow.
            logger.LogWarning(ex, "Could not persist {Count} analytics events.", accepted);
            return new AnalyticsIngestResult(0, events.Count, "storage_unavailable");
        }

        return new AnalyticsIngestResult(accepted, dropped, null);
    }

    /// <summary>
    /// Keeps only the property keys the catalog permits for this event, coerces values
    /// to short primitives, and passes the result through the secret redactor.
    /// </summary>
    private static string? BuildProperties(
        AnalyticsEventDefinition definition,
        Dictionary<string, JsonElement>? properties)
    {
        if (properties is null || properties.Count == 0 || definition.AllowedProperties.Count == 0)
        {
            return null;
        }

        var result = new JsonObject();

        foreach (var (key, value) in properties)
        {
            if (result.Count >= AnalyticsEventCatalog.MaxPropertiesPerEvent)
            {
                break;
            }

            if (!definition.AllowedProperties.Contains(key))
            {
                continue;
            }

            var node = CoercePrimitive(value);
            if (node is not null)
            {
                result[key] = node;
            }
        }

        if (result.Count == 0)
        {
            return null;
        }

        return ErrorSanitizer.SanitizeJson(result.ToJsonString());
    }

    private static JsonNode? CoercePrimitive(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => JsonValue.Create(Truncate(
            value.GetString(),
            AnalyticsEventCatalog.MaxPropertyValueLength)),
        JsonValueKind.Number when value.TryGetDouble(out var number) => JsonValue.Create(number),
        JsonValueKind.True => JsonValue.Create(true),
        JsonValueKind.False => JsonValue.Create(false),
        // Objects and arrays are rejected outright so nested payloads cannot smuggle
        // content past the allowlist.
        _ => null
    };

    private static string? ResolveCountryCode(HttpContext http)
    {
        var value = http.Request.Headers["X-Country-Code"].ToString();
        if (string.IsNullOrWhiteSpace(value) || value.Length != 2)
        {
            return null;
        }

        return value.All(char.IsAsciiLetter) ? value.ToUpperInvariant() : null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public sealed record AnalyticsEventInput(
    string? EventName,
    Dictionary<string, JsonElement>? Properties,
    string? AppVersion);

public sealed record AnalyticsIngestResult(int Accepted, int Dropped, string? Reason);
