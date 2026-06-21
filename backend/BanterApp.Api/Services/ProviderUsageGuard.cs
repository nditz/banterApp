using BanterApp.Api.Data;
using BanterApp.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Services;

public interface IProviderUsageGuard
{
    Task<bool> CanInvokeAsync(string provider, int estimatedUnits = 1, CancellationToken ct = default);
    Task RecordSuccessAsync(string provider, int estimatedUnits = 1, int latencyMs = 0, CancellationToken ct = default);
    Task RecordFailureAsync(string provider, string message, CancellationToken ct = default);
    Task<ProviderUsageSummary> GetTodaySummaryAsync(string provider, CancellationToken ct = default);
    bool IsCircuitOpen(string provider);
}

public sealed record ProviderUsageSummary(
    string Provider,
    int RequestsToday,
    int FailuresToday,
    int EstimatedUnitsToday,
    double? AverageLatencyMs,
    bool CircuitOpen);

public sealed class ProviderUsageGuard(
    AppDbContext db,
    IConfiguration configuration,
    IApplicationErrorLogger errorLogger) : IProviderUsageGuard
{
    private readonly Dictionary<string, CircuitState> _circuits = new(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> CanInvokeAsync(string provider, int estimatedUnits = 1, CancellationToken ct = default)
    {
        if (IsCircuitOpen(provider))
        {
            return false;
        }

        var dailyLimit = configuration.GetValue($"ProviderUsage:{provider}:DailyLimit", 0);
        if (dailyLimit <= 0)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usage = await db.ProviderUsageDaily
            .FirstOrDefaultAsync(u => u.Provider == provider && u.UsageDate == today, ct);

        return usage is null || usage.RequestCount + estimatedUnits <= dailyLimit;
    }

    public async Task RecordSuccessAsync(string provider, int estimatedUnits = 1, int latencyMs = 0, CancellationToken ct = default)
    {
        await UpsertUsageAsync(provider, success: true, estimatedUnits, latencyMs, ct);
        ResetCircuit(provider);
    }

    public async Task RecordFailureAsync(string provider, string message, CancellationToken ct = default)
    {
        await UpsertUsageAsync(provider, success: false, units: 0, latencyMs: 0, ct);
        await errorLogger.LogAsync(
            "provider",
            message,
            category: "provider_failure",
            ct: ct);

        var threshold = configuration.GetValue($"ProviderUsage:{provider}:FailureThreshold", 5);
        var windowMinutes = configuration.GetValue($"ProviderUsage:{provider}:FailureWindowMinutes", 5);
        var state = GetCircuit(provider);
        state.Failures.Add(DateTimeOffset.UtcNow);
        state.Failures.RemoveAll(t => t < DateTimeOffset.UtcNow.AddMinutes(-windowMinutes));
        if (state.Failures.Count >= threshold)
        {
            state.OpenUntil = DateTimeOffset.UtcNow.AddMinutes(
                configuration.GetValue($"ProviderUsage:{provider}:CircuitBreakMinutes", 10));
        }
    }

    public async Task<ProviderUsageSummary> GetTodaySummaryAsync(string provider, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usage = await db.ProviderUsageDaily
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Provider == provider && u.UsageDate == today, ct);

        return new ProviderUsageSummary(
            provider,
            usage?.RequestCount ?? 0,
            usage?.FailureCount ?? 0,
            usage?.EstimatedUnits ?? 0,
            usage?.AverageLatencyMs,
            IsCircuitOpen(provider));
    }

    public bool IsCircuitOpen(string provider)
    {
        var state = GetCircuit(provider);
        return state.OpenUntil.HasValue && state.OpenUntil > DateTimeOffset.UtcNow;
    }

    private async Task UpsertUsageAsync(
        string provider,
        bool success,
        int units,
        int latencyMs,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var usage = await db.ProviderUsageDaily
            .FirstOrDefaultAsync(u => u.Provider == provider && u.UsageDate == today, ct);

        if (usage is null)
        {
            usage = new ProviderUsageDaily
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                UsageDate = today
            };
            db.ProviderUsageDaily.Add(usage);
        }

        if (success)
        {
            usage.RequestCount++;
            usage.EstimatedUnits += units;
            if (latencyMs > 0)
            {
                usage.TotalLatencyMs += latencyMs;
                usage.LatencySamples++;
                usage.AverageLatencyMs = usage.TotalLatencyMs / (double)usage.LatencySamples;
            }
        }
        else
        {
            usage.FailureCount++;
        }

        usage.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private CircuitState GetCircuit(string provider)
    {
        if (!_circuits.TryGetValue(provider, out var state))
        {
            state = new CircuitState();
            _circuits[provider] = state;
        }

        return state;
    }

    private void ResetCircuit(string provider)
    {
        if (_circuits.TryGetValue(provider, out var state))
        {
            state.Failures.Clear();
            state.OpenUntil = null;
        }
    }

    private sealed class CircuitState
    {
        public List<DateTimeOffset> Failures { get; } = [];
        public DateTimeOffset? OpenUntil { get; set; }
    }
}
