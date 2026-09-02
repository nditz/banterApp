namespace BanterApp.Api.Integrations.Banter;

public sealed class SystemBanterRandom : IBanterRandom
{
    public double NextDouble() => Random.Shared.NextDouble();

    public int Next(int maxExclusive) =>
        maxExclusive <= 0 ? 0 : Random.Shared.Next(maxExclusive);
}

/// <summary>Seedable RNG for unit tests.</summary>
public sealed class SeededBanterRandom : IBanterRandom
{
    private readonly Random _random;

    public SeededBanterRandom(int seed) => _random = new Random(seed);

    public double NextDouble() => _random.NextDouble();

    public int Next(int maxExclusive) =>
        maxExclusive <= 0 ? 0 : _random.Next(maxExclusive);
}
