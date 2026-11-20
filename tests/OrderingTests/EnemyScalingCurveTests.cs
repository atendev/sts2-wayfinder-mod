using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class EnemyScalingCurveTests
{
    private const int PathLength = 50;

    [Theory]
    [InlineData(0, 1, 1.0)]
    [InlineData(0, PathLength, 1.0)]
    [InlineData(1, 1, 1.0)]
    [InlineData(1, PathLength, 1.3)]
    [InlineData(2, 1, 1.3)]
    [InlineData(2, PathLength, 1.5)]
    public void PathHpHitsTheCurveEnds(int act, int row, double expected) =>
        Assert.Equal((decimal)expected, EnemyScalingCurve.PathHp(act, row, PathLength));

    [Theory]
    [InlineData(1, 1, 1.0)]
    [InlineData(1, PathLength, 1.2)]
    [InlineData(2, 1, 1.2)]
    [InlineData(2, PathLength, 1.5)]
    public void PathDamageHitsTheCurveEnds(int act, int row, double expected) =>
        Assert.Equal((decimal)expected, EnemyScalingCurve.PathDamage(act, row, PathLength));

    [Fact]
    public void PathRisesWithoutAJumpAtTheActChange()
    {
        var rising = Enumerable.Range(1, PathLength).Select(row => EnemyScalingCurve.PathHp(1, row, PathLength))
            .Concat(Enumerable.Range(1, PathLength).Select(row => EnemyScalingCurve.PathHp(2, row, PathLength)))
            .ToList();

        Assert.Equal(rising.OrderBy(value => value), rising);
    }

    [Fact]
    public void BossesUseOneValuePerAct()
    {
        Assert.Equal([1m, 1.3m, 1.5m], new[] { 0, 1, 2 }.Select(EnemyScalingCurve.BossHpFor));
        Assert.Equal([1m, 1.1m, 1.2m], new[] { 0, 1, 2 }.Select(EnemyScalingCurve.BossDamageFor));
    }

    [Fact]
    public void ShortPathStillReachesTheEndValue() =>
        Assert.Equal(1.3m, EnemyScalingCurve.PathHp(1, 1, 1));
}
