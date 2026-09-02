namespace BanterApp.Api.Integrations.Media;

/// <summary>
/// Tracks GIF, meme, and sticker identities used in the current Friday–Monday window so the
/// same visual is not assigned twice. Seed lookups keep a given feed card stable after the first pick.
/// </summary>
public interface IReactionGifLedger
{
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    Task<string?> GetAssignedUrlAsync(int seed, CancellationToken cancellationToken = default);

    Task<bool> TryClaimAsync(int seed, string gifId, string url, CancellationToken cancellationToken = default);
}
