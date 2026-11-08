namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

/// <summary>A deterministic stream keyed by a run's seed text and a name. It uses no game RNG type, because the numeric seed is a uint on stable and a ulong on public-beta.</summary>
internal sealed class SeededOrderingRng(string seed, string name) : IOrderingRng
{
    private const ulong FnvOffset = 0xCBF29CE484222325UL;
    private const ulong FnvPrime = 0x100000001B3UL;
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    private ulong _state = Hash(seed, name);

    public int NextInt(int maxExclusive)
    {
        _state += GoldenGamma;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (int)(z % (ulong)maxExclusive);
    }

    private static ulong Hash(string seed, string name)
    {
        var hash = FnvOffset;
        foreach (var c in $"{seed}\0{name}")
            hash = (hash ^ c) * FnvPrime;

        return hash;
    }
}
