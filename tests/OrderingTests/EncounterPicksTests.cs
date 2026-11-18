using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;
using Xunit;

namespace OrderingTests;

public class EncounterPicksTests
{
    private sealed record Enc(string Name);

    private static readonly Enc A = new("A"), B = new("B"), C = new("C");
    private static readonly Enc[] ElitePool = [A, B, C];
    private static readonly Enc[] Regular = [new("r1"), new("r2"), new("r3"), new("r4"), new("r5")];

    [Fact]
    public void EliteListOpensWithEveryPoolEliteOnceInASeededShuffle()
    {
        var openings = Enumerable.Range(0, 30)
            .Select(seed => EncounterPicks.BuildEliteList([A, A, A], ElitePool, 20, new FixedRng(seed)).Take(3).Select(e => e.Name).ToList())
            .ToList();

        Assert.All(openings, opening => Assert.Equal(["A", "B", "C"], opening.Order()));
        Assert.True(openings.Select(o => string.Concat(o)).Distinct().Count() > 2, "the opening order should vary with the seed");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(99)]
    public void EliteListNeverRepeatsTheEliteJustFought(int seed)
    {
        var list = EncounterPicks.BuildEliteList([A, B, C], ElitePool, EncounterPicks.EliteListLength, new FixedRng(seed));

        Assert.Equal(EncounterPicks.EliteListLength, list.Count);
        for (var i = 1; i < list.Count; i++)
            Assert.NotSame(list[i - 1], list[i]);
    }

    [Fact]
    public void EliteListPicksAfterTheOpeningAreNotAPermutationCycle()
    {
        var tails = Enumerable.Range(0, 40)
            .Select(seed => EncounterPicks.BuildEliteList([A, B, C], ElitePool, 9, new FixedRng(seed)).Skip(3).Select(e => e.Name).ToList())
            .ToList();

        Assert.True(tails.Select(tail => string.Concat(tail)).Distinct().Count() > 6, "the picks after the opening should vary");
        Assert.Contains(tails, tail => tail.Take(3).Distinct().Count() < 3);
    }

    [Fact]
    public void NormalListKeepsTheVanillaListThenAddsOnlyRegularEncounters()
    {
        var weak = new Enc("weak");
        Enc[] vanilla = [weak, weak, Regular[0], Regular[1], Regular[2]];
        var list = EncounterPicks.ExtendNormalList(vanilla, Regular, EncounterPicks.NormalListLength, new FixedRng(5));

        Assert.Equal(EncounterPicks.NormalListLength, list.Count);
        Assert.Equal(vanilla, list.Take(vanilla.Length));
        Assert.All(list.Skip(vanilla.Length), enc => Assert.Contains(enc, Regular));
        Assert.DoesNotContain(weak, list.Skip(vanilla.Length));
    }

    [Fact]
    public void NormalListNeverPicksTheSameEncounterTwiceInARow()
    {
        var list = EncounterPicks.ExtendNormalList([Regular[0]], Regular, EncounterPicks.NormalListLength, new FixedRng(8));

        for (var i = 1; i < list.Count; i++)
            Assert.NotSame(list[i - 1], list[i]);
    }

    [Fact]
    public void ListsAreTheSameForTheSameSeed()
    {
        var first = EncounterPicks.ExtendNormalList([Regular[0]], Regular, 40, new FixedRng(11));
        var second = EncounterPicks.ExtendNormalList([Regular[0]], Regular, 40, new FixedRng(11));

        Assert.Equal(first, second);
    }

    [Fact]
    public void AnEmptyPoolLeavesTheVanillaListUnchanged()
    {
        Enc[] vanilla = [A, B];

        Assert.Equal(vanilla, EncounterPicks.BuildEliteList(vanilla, [], 20, new FixedRng(1)));
        Assert.Equal(vanilla, EncounterPicks.ExtendNormalList(vanilla, [], 20, new FixedRng(1)));
    }

    [Fact]
    public void AOneEncounterPoolRepeatsItBecauseThereIsNothingElse()
    {
        var list = EncounterPicks.ExtendNormalList([Regular[0]], [Regular[0]], 5, new FixedRng(1));

        Assert.All(list, enc => Assert.Same(Regular[0], enc));
    }
}
