using sts2_wayfinder_mod.sts2_wayfinder_modCode.Layout;
using Xunit;

namespace OrderingTests;

public class SwitchbackLayoutTests
{
    private const int MaxPoints = 120;
    private const float LeftEdge = -500f;
    private const float RightEdge = 400f;
    private const float Center = -50f;

    private static IEnumerable<(int Points, bool SecondBoss)> AllCases() =>
        Enumerable.Range(SwitchbackLayout.MinPoints, MaxPoints - SwitchbackLayout.MinPoints + 1)
            .SelectMany(points => new[] { (points, false), (points, true) });

    private static List<List<(float X, float Y)>> Ranks(int points, bool secondBoss) =>
        SwitchbackLayout.Positions(points, secondBoss)
            .GroupBy(position => position.Y)
            .Select(rank => rank.ToList())
            .ToList();

    [Fact]
    public void EveryPointGetsOnePosition()
    {
        foreach (var (points, secondBoss) in AllCases())
            Assert.Equal(points, SwitchbackLayout.Positions(points, secondBoss).Count);
    }

    [Fact]
    public void PathRunsFromBottomCenterToTopCenter()
    {
        foreach (var (points, secondBoss) in AllCases())
        {
            var positions = SwitchbackLayout.Positions(points, secondBoss);
            var topY = 740f - 2325f * (secondBoss ? 0.9f : 1f);

            Assert.Equal((Center, 585f), positions[0]);
            Assert.Equal(Center, positions[^1].X, 0.01f);
            Assert.Equal(topY, positions[^1].Y, 0.01f);
        }
    }

    [Fact]
    public void EndRanksHoldFourPointsAndMiddleRanksHoldSixToEight()
    {
        foreach (var (points, secondBoss) in AllCases())
        {
            var ranks = Ranks(points, secondBoss);

            Assert.Equal(4, ranks[0].Count);
            Assert.Equal(4, ranks[^1].Count);
            Assert.All(ranks[1..^1], rank => Assert.InRange(rank.Count, 6, 8));
        }
    }

    [Fact]
    public void MiddleRanksSpanTheFullWidth()
    {
        foreach (var (points, secondBoss) in AllCases())
        {
            foreach (var rank in Ranks(points, secondBoss)[1..^1])
            {
                Assert.Equal(LeftEdge, rank.Min(position => position.X), 0.01f);
                Assert.Equal(RightEdge, rank.Max(position => position.X), 0.01f);
            }
        }
    }

    [Fact]
    public void EachRankStartsWhereThePreviousRankEnded()
    {
        foreach (var (points, secondBoss) in AllCases())
        {
            var ranks = Ranks(points, secondBoss);
            for (var i = 1; i < ranks.Count; i++)
                Assert.Equal(ranks[i - 1][^1].X, ranks[i][0].X, 0.01f);
        }
    }

    [Fact]
    public void PointsInARankAreEvenlySpacedWithinTheDecidedRange()
    {
        foreach (var (points, secondBoss) in AllCases())
        {
            foreach (var rank in Ranks(points, secondBoss))
            {
                var gaps = rank.Zip(rank.Skip(1), (a, b) => Math.Abs(b.X - a.X)).ToList();
                Assert.All(gaps, gap => Assert.Equal(gaps[0], gap, 0.01f));
                Assert.InRange(gaps[0], 128f, 180.01f);
            }
        }
    }

    [Theory]
    [InlineData(57, false, 271.25f)]
    [InlineData(60, false, 271.25f)]
    [InlineData(66, false, 241.11f)]
    [InlineData(57, true, 242.19f)]
    [InlineData(66, true, 215.28f)]
    public void RanksDivideTheHeightEvenly(int points, bool secondBoss, float expectedGap)
    {
        var ranks = Ranks(points, secondBoss);
        for (var i = 1; i < ranks.Count; i++)
            Assert.Equal(expectedGap, ranks[i - 1][0].Y - ranks[i][0].Y, 0.01f);
    }

    [Fact]
    public void FewerPointsThanTheMinimumAreRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SwitchbackLayout.Positions(SwitchbackLayout.MinPoints - 1, false));
}
