using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;
using Xunit;

namespace OrderingTests;

public class SeededOrderingRngTests
{
    private static int[] Draws(string seed, string name, int count = 32)
    {
        var rng = new SeededOrderingRng(seed, name);
        return Enumerable.Range(0, count).Select(_ => rng.NextInt(1000)).ToArray();
    }

    [Fact]
    public void SameSeedAndNameGiveTheSameDraws() =>
        Assert.Equal(Draws("ABC123", "act_1_wayfinder"), Draws("ABC123", "act_1_wayfinder"));

    [Fact]
    public void DifferentNamesGiveDifferentDraws() =>
        Assert.NotEqual(Draws("ABC123", "act_1_wayfinder"), Draws("ABC123", "act_2_wayfinder"));

    [Fact]
    public void DifferentSeedsGiveDifferentDraws() =>
        Assert.NotEqual(Draws("ABC123", "act_1_wayfinder"), Draws("ABC124", "act_1_wayfinder"));

    [Fact]
    public void DrawsMatchAPinnedSequence() =>
        Assert.Equal([720, 853, 870, 129], Draws("ABC123", "act_1_wayfinder", 4));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public void DrawsStayBelowTheBound(int maxExclusive)
    {
        var rng = new SeededOrderingRng("ABC123", "act_1_wayfinder");
        for (var i = 0; i < 1000; i++)
            Assert.InRange(rng.NextInt(maxExclusive), 0, maxExclusive - 1);
    }
}
