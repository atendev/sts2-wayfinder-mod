using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class EliteRelicRarityTests
{
    [Theory]
    [InlineData(0f, (int)EliteRelicTier.Common)]
    [InlineData(0.74f, (int)EliteRelicTier.Common)]
    [InlineData(0.75f, (int)EliteRelicTier.Uncommon)]
    [InlineData(0.94f, (int)EliteRelicTier.Uncommon)]
    [InlineData(0.95f, (int)EliteRelicTier.Rare)]
    [InlineData(0.99f, (int)EliteRelicTier.Rare)]
    public void RollsMapToTiers(float roll, int expected) => Assert.Equal(expected, (int)EliteRelicRarity.Tier(roll));
}
