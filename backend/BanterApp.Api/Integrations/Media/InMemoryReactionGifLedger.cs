using System.Collections.Concurrent;

namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Process-local uniqueness ledger used in tests and as a stand-in when a database-backed
/// ledger is not wired up.
/// </summary>
public sealed class InMemoryReactionGifLedger : IReactionGifLedger
{
    private readonly ConcurrentDictionary<string, byte> _ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, string> _seeds = new();

    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<string?> GetAssignedUrlAsync(int seed, CancellationToken cancellationToken = default) =>
        Task.FromResult(_seeds.TryGetValue(seed, out var url) ? url : null);

    public Task<bool> TryClaimAsync(
        int seed,
        string gifId,
        string url,
        CancellationToken cancellationToken = default)
    {
        if (!_ids.TryAdd(gifId, 0))
        {
            return Task.FromResult(false);
        }

        _seeds[seed] = url;
        return Task.FromResult(true);
    }
}
