using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class CardRewardScheduleTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(7, false)]
    public void NormalCombatsAlternateStartingWithAReward(int earlierNormalCombats, bool expected) =>
        Assert.Equal(expected, CardRewardSchedule.GivesCardReward(true, true, earlierNormalCombats));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ElitesAndBossesAlwaysGiveOne(int earlierNormalCombats) =>
        Assert.True(CardRewardSchedule.GivesCardReward(true, false, earlierNormalCombats));

    [Fact]
    public void WithoutAlternationEveryCombatGivesOne() =>
        Assert.True(CardRewardSchedule.GivesCardReward(false, true, 1));
}
