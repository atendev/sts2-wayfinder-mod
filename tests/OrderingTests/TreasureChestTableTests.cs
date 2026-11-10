using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class TreasureChestTableTests
{
    private static readonly TreasureOdds Odds = TreasureOdds.Balanced;

    [Fact]
    public void FirstChestUsesTheStartChance() =>
        Assert.Equal(0.2m, TreasureChestTable.RelicChance(Odds, []));

    [Fact]
    public void EachGoldChestAddsAStep() =>
        Assert.Equal(0.6m, TreasureChestTable.RelicChance(Odds, [ChestResult.Gold, ChestResult.Gold]));

    [Fact]
    public void ARelicChestResetsTheChance() =>
        Assert.Equal(0.2m, TreasureChestTable.RelicChance(Odds, [ChestResult.Gold, ChestResult.Gold, ChestResult.Relic]));

    [Fact]
    public void AnEmptyChestIsNotAMiss() =>
        Assert.Equal(0.4m, TreasureChestTable.RelicChance(Odds, [ChestResult.Gold, ChestResult.Empty]));

    [Fact]
    public void TheChanceCapsAtOne() =>
        Assert.Equal(1m, TreasureChestTable.RelicChance(Odds, Enumerable.Repeat(ChestResult.Gold, 9)));

    [Theory]
    [InlineData(0.19f, true)]
    [InlineData(0.2f, false)]
    [InlineData(0.99f, false)]
    public void TheRollDecidesBelowTheChance(float roll, bool expected) =>
        Assert.Equal(expected, TreasureChestTable.IsRelicChest(Odds, [], false, roll));

    [Fact]
    public void TheLastChestOfTheActIsAlwaysARelicChest() =>
        Assert.True(TreasureChestTable.IsRelicChest(Odds, [ChestResult.Gold], true, 0.999f));

    [Theory]
    [InlineData(0.25, 0.25, 0.5)]
    [InlineData(0.15, 0.15, 0.3)]
    public void PresetsStepFromTheirStart(double start, double step, double second) =>
        Assert.Equal((decimal)second, TreasureChestTable.RelicChance(
            new TreasureOdds(true, (decimal)start, (decimal)step, 2m), [ChestResult.Gold]));
}
