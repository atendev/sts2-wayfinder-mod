using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace OrderingTests;

internal sealed class FixedRng(int seed) : IOrderingRng
{
    private ulong _state = (ulong)seed * 0x9E3779B97F4A7C15UL + 0x1234567UL;

    public int NextInt(int maxExclusive)
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (int)(z % (ulong)maxExclusive);
    }
}
