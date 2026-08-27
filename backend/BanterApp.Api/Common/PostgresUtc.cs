using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BanterApp.Api.Common;

/// <summary>
/// Npgsql rejects DateTimeOffset values with a non-zero offset when writing
/// PostgreSQL <c>timestamp with time zone</c>. Convert to UTC before persist.
/// </summary>
public static class PostgresUtc
{
    public static DateTimeOffset Normalize(DateTimeOffset value) =>
        value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();

    public static DateTimeOffset? Normalize(DateTimeOffset? value) =>
        value is { } inner ? Normalize(inner) : null;

    public static void NormalizeTrackedEntities(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTimeOffset value && value.Offset != TimeSpan.Zero)
                {
                    property.CurrentValue = Normalize(value);
                }
            }
        }
    }
}
