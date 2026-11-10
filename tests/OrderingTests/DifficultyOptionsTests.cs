using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class DifficultyOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EachPresetMatchesItself(int preset) =>
        Assert.Equal((DifficultyPreset)preset, DifficultyOptions.For((DifficultyPreset)preset).Preset);

    [Fact]
    public void BossesOnlyScalesNothingButBosses()
    {
        var options = DifficultyOptions.BossesOnly;
        Assert.Equal(new EnemyScalingToggles(true, false, false), options.EnemyScaling);
        Assert.False(options.Treasure.Enabled);
        Assert.False(options.AlternateCardRewards);
        Assert.Equal(3, options.CardChoices);
    }

    [Fact]
    public void PowerCurveUsesBalancedChestsAndFourCardChoices()
    {
        var options = DifficultyOptions.PowerCurve;
        Assert.Equal(TreasureOdds.Balanced, options.Treasure);
        Assert.Equal(4, options.CardChoices);
        Assert.Equal(1m, options.GoldMultiplier);
    }

    [Fact]
    public void ChangingAnyOptionMakesItCustom() =>
        Assert.Equal(DifficultyPreset.Custom, (DifficultyOptions.PowerCurve with { GoldRate = GoldRate.Rich }).Preset);

    [Fact]
    public void ChestOddsDoNotMatterWhileChestChancesAreOff() =>
        Assert.Equal(DifficultyPreset.Off, (DifficultyOptions.Off with { ChestOdds = ChestOdds.Stingy }).Preset);

    [Fact]
    public void ChestOddsMatterWhileChestChancesAreOn() =>
        Assert.Equal(DifficultyPreset.Custom, (DifficultyOptions.PowerCurve with { ChestOdds = ChestOdds.Stingy }).Preset);

    [Theory]
    [InlineData(0, 0.75)]
    [InlineData(1, 1)]
    [InlineData(2, 1.25)]
    public void GoldRateIsAMultiplier(int rate, double expected) =>
        Assert.Equal((decimal)expected, (DifficultyOptions.Off with { GoldRate = (GoldRate)rate }).GoldMultiplier);

    [Fact]
    public void ASnapshotRoundTripsThroughJson()
    {
        var options = DifficultyOptions.PowerCurve with { ChestOdds = ChestOdds.Stingy, GoldRate = GoldRate.Lean, CardChoices = 5 };
        Assert.Equal(options, DifficultyOptions.FromJson(options.ToJson()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    public void AMissingOrBrokenSnapshotReadsAsOff(string? json) =>
        Assert.Equal(DifficultyOptions.Off, DifficultyOptions.FromJson(json));

    [Fact]
    public void CardChoicesFromASnapshotStayInRange() =>
        Assert.Equal(5, DifficultyOptions.FromJson((DifficultyOptions.Off with { CardChoices = 9 }).ToJson()).CardChoices);
}
