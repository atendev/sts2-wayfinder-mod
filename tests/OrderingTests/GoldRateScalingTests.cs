using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class GoldRateScalingTests
{
    [Theory]
    [InlineData(20, 0.75, 15)]
    [InlineData(25, 0.75, 19)]
    [InlineData(25, 1.25, 31)]
    [InlineData(100, 1.25, 125)]
    public void ScalesAndRoundsAwayFromZero(int amount, double multiplier, int expected) =>
        Assert.Equal(expected, GoldRateScaling.Apply(amount, (decimal)multiplier));

    [Fact]
    public void VanillaLeavesTheAmountAlone() =>
        Assert.Equal(37m, GoldRateScaling.Apply(37m, 1m));

    [Fact]
    public void ZeroStaysZero() =>
        Assert.Equal(0m, GoldRateScaling.Apply(0m, 1.25m));
}
