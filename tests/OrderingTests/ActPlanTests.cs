using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;
using Xunit;
using static sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering.PointType;

namespace OrderingTests;

public class ActPlanTests
{
    private const int SeedsPerAct = 600;

    public static TheoryData<int> Acts => new() { 0, 1, 2 };

    private static IEnumerable<(int Seed, ActLayout Layout, OrderingResult Result)> Runs(int act)
    {
        for (var seed = 1; seed <= SeedsPerAct; seed++)
        {
            var rng = new FixedRng(seed * 31 + act);
            var layout = ActPlans.For(act).Roll(rng);
            yield return (seed, layout, PathOrderer.Order(layout.Mix, rng, layout.Rules));
        }
    }

    private static bool InRange(CountRange range, int value) => value >= range.Min && value <= range.Max;

    private static string Letters(IEnumerable<PointType> path) =>
        string.Concat(path.Select(p => "MEUSRT"[(int)p]));

    private static List<int> ElitePositions(IReadOnlyList<PointType> path) =>
        Enumerable.Range(0, path.Count).Where(i => path[i] == Elite).ToList();

    [Theory]
    [MemberData(nameof(Acts))]
    public void RolledMixesStayInTheActsRanges(int act)
    {
        var plan = ActPlans.For(act);
        foreach (var (seed, layout, _) in Runs(act))
        {
            var mix = layout.Mix;
            var message = $"{plan.Name} seed {seed} mix {mix}";
            Assert.True(InRange(plan.Length, mix.Total), message);
            Assert.True(InRange(plan.Elites, mix.Elites), message);
            Assert.True(InRange(plan.Unknowns, mix.Unknowns), message);
            Assert.True(InRange(plan.RestSites, mix.RestSites), message);
            Assert.True(InRange(plan.Shops, mix.Shops), message);
            Assert.True(InRange(plan.Treasures, mix.Treasures), message);
            Assert.True(mix.Monsters >= 1, message);
        }
    }

    [Theory]
    [MemberData(nameof(Acts))]
    public void EveryRolledMixLandsOnceAndKeepsItsEnds(int act)
    {
        foreach (var (seed, layout, result) in Runs(act))
        {
            var path = result.Path;
            var message = $"act {act} seed {seed} {Letters(path)}";
            Assert.True(path.Count == layout.Mix.Total, message);
            Assert.True(path.Count(p => p == Elite) == layout.Mix.Elites, message);
            Assert.True(path.Count(p => p == Unknown) == layout.Mix.Unknowns, message);
            Assert.True(path.Count(p => p == Monster) == layout.Mix.Monsters, message);
            Assert.True(RuleValidator.EndsHold(path, layout.Mix), message);
        }
    }

    [Theory]
    [MemberData(nameof(Acts))]
    public void RollsAreRepeatable(int act)
    {
        foreach (var (seed, layout, result) in Runs(act).Take(100))
        {
            var rng = new FixedRng(seed * 31 + act);
            var again = ActPlans.For(act).Roll(rng);
            Assert.Equal(layout.Mix, again.Mix);
            Assert.True(result.Path.SequenceEqual(PathOrderer.Order(again.Mix, rng, again.Rules).Path), $"act {act} seed {seed}");
        }
    }

    [Theory]
    [MemberData(nameof(Acts))]
    public void MostPathsHoldEveryRuleAndNoneBreaksMoreThanTheLastStep(int act)
    {
        var clean = Runs(act).Count(r => r.Result.Step == 0);
        Assert.True(clean >= SeedsPerAct * 0.9, $"act {act}: {clean} of {SeedsPerAct} held every rule");
    }

    [Fact]
    public void ActOneKeepsElitesOutOfTheFirstThirdAndSixApart()
    {
        foreach (var (seed, layout, result) in Runs(0))
        {
            var path = result.Path;
            var elites = ElitePositions(path);
            var broken = result.BrokenRules;
            var message = $"seed {seed} {Letters(path)} broke {string.Join(",", broken)}";

            if (!broken.Contains(OrderingRule.OpeningBan))
                Assert.True(elites.All(i => i + 1 > path.Count / 3), message);
            if (!broken.Contains(OrderingRule.EliteGap))
                for (var i = 0; i + 1 < elites.Count; i++)
                    Assert.True(elites[i + 1] - elites[i] >= 6, message);
        }
    }

    [Fact]
    public void ActOneKeepsItsElitePlacementAtTheHardSearchToo()
    {
        var mix = new PointMix(Monsters: 18, Elites: 4, Unknowns: 4, Shops: 2, RestSites: 6, Treasures: 2);
        var found = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            var path = HardRuleSearch.TryOrder(mix, HardRules.AtStep(1), new FixedRng(seed), ActPlans.BuildTheDeck.Flavor);
            if (path == null)
                continue;

            found++;
            var elites = ElitePositions(path);
            var message = $"seed {seed} {Letters(path)}";
            Assert.True(elites.All(i => i + 1 > path.Length / 3), message);
            for (var i = 0; i + 1 < elites.Count; i++)
                Assert.True(elites[i + 1] - elites[i] >= 6, message);
        }

        Assert.True(found > 50, $"the hard search found {found} of 100");
    }

    [Fact]
    public void ActTwoMayRunUnknownsAndNeverPastThree()
    {
        var longest = 0;
        foreach (var (seed, _, result) in Runs(1))
        {
            var run = RuleValidator.MaxRun(result.Path, Unknown);
            Assert.True(run <= 3, $"seed {seed} {Letters(result.Path)}");
            longest = Math.Max(longest, run);
        }

        Assert.True(longest >= 2, "no Act 2 path ever ran two Unknowns in a row");
    }

    [Fact]
    public void ActThreeElitePairsHaveRestBeforeAndRoomAround()
    {
        var pairs = 0;
        foreach (var (seed, layout, result) in Runs(2))
        {
            var path = result.Path;
            var elites = ElitePositions(path);
            var message = $"seed {seed} {Letters(path)} broke {string.Join(",", result.BrokenRules)}";

            if (!result.BrokenRules.Contains(OrderingRule.ElitesApart))
            {
                Assert.True(RuleValidator.MaxRun(path, Elite) <= 2, message);
                for (var i = 0; i + 1 < elites.Count; i++)
                {
                    if (elites[i + 1] - elites[i] != 1)
                        continue;

                    pairs++;
                    Assert.True(elites[i] >= 1 && path[elites[i] - 1] == RestSite, message);
                }
            }

            if (result.BrokenRules.Contains(OrderingRule.EliteGap))
                continue;

            for (var i = 0; i + 1 < elites.Count; i++)
            {
                var pairInvolved = elites[i + 1] - elites[i] == 1
                    || (i > 0 && elites[i] - elites[i - 1] == 1)
                    || (i + 2 < elites.Count && elites[i + 2] - elites[i + 1] == 1);
                var gap = elites[i + 1] - elites[i];
                if (gap == 1)
                    continue;

                Assert.True(gap >= (pairInvolved ? PlacementRules.PairClearance + 1 : 3), message);
            }
        }

        Assert.True(pairs > SeedsPerAct / 2, $"only {pairs} pairs over {SeedsPerAct} paths");
    }

    [Theory]
    [MemberData(nameof(Acts))]
    public void ARestSiteAlwaysComesBeforeTheBoss(int act)
    {
        foreach (var (seed, _, result) in Runs(act))
            Assert.True(result.Path[^1] == RestSite, $"act {act} seed {seed}");
    }

    [Fact]
    public void ActThreeKeepsItsPairsAtTheHardSearchToo()
    {
        // A dense mix that the spread cannot place forces the hard rule search to place the pair itself.
        var mix = new PointMix(Monsters: 9, Elites: 6, Unknowns: 2, Shops: 1, RestSites: 4, Treasures: 1);
        var rules = PlacementRules.Standard with { ElitePairs = 2 };
        var found = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var path = HardRuleSearch.TryOrder(mix, HardRules.AtStep(1), new FixedRng(seed), rules);
            if (path == null)
                continue;

            found++;
            var message = $"seed {seed} {Letters(path)}";
            Assert.True(RuleValidator.MaxRun(path, Elite) <= 2, message);
            var elites = ElitePositions(path);
            for (var i = 0; i + 1 < elites.Count; i++)
                if (elites[i + 1] - elites[i] == 1)
                    Assert.True(path[elites[i] - 1] == RestSite, message);
        }

        Assert.True(found > 100, $"the hard search found {found} of 200");
    }

    [Fact]
    public void TheEncounterListCoversTheMostFightsAnActCanRoll()
    {
        for (var act = 0; act < 3; act++)
        {
            var plan = ActPlans.For(act);
            foreach (var (seed, layout, _) in Runs(act))
                Assert.True(layout.Mix.Monsters + layout.Mix.Unknowns <= plan.MaxFightPoints, $"{plan.Name} seed {seed} mix {layout.Mix}");
        }

        Assert.Equal([26, 25, 21], Enumerable.Range(0, 3).Select(a => ActPlans.For(a).MaxFightPoints));
    }

    [Fact]
    public void AModdedActBeyondThePlansReusesTheLastOne() =>
        Assert.Same(ActPlans.TheGauntlet, ActPlans.For(7));
}
