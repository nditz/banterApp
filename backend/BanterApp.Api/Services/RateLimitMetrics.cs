namespace BanterApp.Api.Services;

public interface IRateLimitMetrics
{
    void RecordRejection(string policyName, string partitionKey);
    int GetTodayRejections(string? policyName = null);
}

public sealed class RateLimitMetrics : IRateLimitMetrics
{
    private readonly object _lock = new();
    private readonly Dictionary<string, int> _today = new(StringComparer.OrdinalIgnoreCase);
    private DateOnly _day = DateOnly.FromDateTime(DateTime.UtcNow);

    public void RecordRejection(string policyName, string partitionKey)
    {
        lock (_lock)
        {
            ResetIfNewDay();
            var key = string.IsNullOrWhiteSpace(policyName) ? "global" : policyName;
            _today.TryGetValue(key, out var count);
            _today[key] = count + 1;
        }
    }

    public int GetTodayRejections(string? policyName = null)
    {
        lock (_lock)
        {
            ResetIfNewDay();
            if (string.IsNullOrWhiteSpace(policyName))
            {
                return _today.Values.Sum();
            }

            return _today.TryGetValue(policyName, out var count) ? count : 0;
        }
    }

    public IReadOnlyDictionary<string, int> GetTodayByPolicy()
    {
        lock (_lock)
        {
            ResetIfNewDay();
            return new Dictionary<string, int>(_today, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void ResetIfNewDay()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (today == _day)
        {
            return;
        }

        _day = today;
        _today.Clear();
    }
}
