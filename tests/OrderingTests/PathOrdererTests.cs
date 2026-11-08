using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;
using Xunit;
using static sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering.PointType;

namespace OrderingTests;

public class PathOrdererTests
{
    private const int SeedsPerCase = 500;

    private static IEnumerable<(string Name, PointMix Mix, int Seed)> Cases(
        IEnumerable<(string Name, Func<FixedRng, PointMix> Make)> groups)
    {
        foreach (var (name, make) in groups)
        {
            var mixRng = new FixedRng(name.GetHashCodeStable());
            for (var seed = 1; seed <= SeedsPerCase; seed++)
                yield return (name, make(mixRng), seed);
        }
    }

    private static IEnumerable<(string Name, PointMix Mix, int Seed)> AllCases() =>
        Cases(TestMixes.Vanilla()).Concat(Cases(TestMixes.Stressed()))
            .Concat(TestMixes.Edge.Select(e => (e.Name, e.Mix, Seed: 1)));

    private static string Letters(IEnumerable<PointType> path) =>
        string.Concat(path.Select(p => p switch
        {
            Monster => 'M',
            Elite => 'E',
            Unknown => 'U',
            Shop => 'S',
            RestSite => 'R',
            Treasure => 'T',
            _ => '?',
        }));

    [Fact]
    public void SameMixAndSeedGiveTheSamePath()
    {
        foreach (var (name, mix, seed) in AllCases())
        {
            var first = PathOrderer.Order(mix, new FixedRng(seed));
            var second = PathOrderer.Order(mix, new FixedRng(seed));
            Assert.True(first.Path.SequenceEqual(second.Path), $"{name} seed {seed} mix {mix}");
        }
    }

    [Fact]
    public void EveryPointLandsExactlyOnce()
    {
        foreach (var (name, mix, seed) in AllCases())
        {
            var path = PathOrderer.Order(mix, new FixedRng(seed)).Path;
            var message = $"{name} seed {seed} mix {mix}";
            Assert.True(path.Count(p => p == Monster) == mix.Monsters, message);
            Assert.True(path.Count(p => p == Elite) == mix.Elites, message);
            Assert.True(path.Count(p => p == Unknown) == mix.Unknowns, message);
            Assert.True(path.Count(p => p == Shop) == mix.Shops, message);
            Assert.True(path.Count(p => p == RestSite) == mix.RestSites, message);
            Assert.True(path.Count(p => p == Treasure) == mix.Treasures, message);
            Assert.True(path.Count == mix.Total, message);
        }
    }

    [Fact]
    public void VanillaMixesBreakNoHardRule()
    {
        foreach (var (name, mix, seed) in Cases(TestMixes.Vanilla()))
        {
            var path = PathOrderer.Order(mix, new FixedRng(seed)).Path;
            var message = $"{name} seed {seed} mix {mix}: {Letters(path)}";
            Assert.True(RuleValidator.EndsHold(path, mix), message);
            Assert.True(RuleValidator.Broken(path).Count == 0,
                $"{message} broke {string.Join(", ", RuleValidator.Broken(path))}");
        }
    }

    [Fact]
    public void StressedMixesBreakOnlyRulesUpToTheStepReached()
    {
        var edge = TestMixes.Edge.Select(e => (e.Name, e.Mix, Seed: 1));
        foreach (var (name, mix, seed) in Cases(TestMixes.Stressed()).Concat(edge))
        {
            var result = PathOrderer.Order(mix, new FixedRng(seed));
            var message = $"{name} seed {seed} mix {mix}: {Letters(result.Path)} step {result.Step}";
            Assert.True(mix.Total == 0 || RuleValidator.EndsHold(result.Path, mix), message);
            foreach (var rule in RuleValidator.Broken(result.Path))
            {
                Assert.True(RuleValidator.StepOf(rule) <= result.Step, $"{message} broke {rule}");
                Assert.True(result.BrokenRules.Contains(rule), $"{message} did not report {rule}");
            }
        }
    }

    [Fact]
    public void SmallMixesMatchBruteForce()
    {
        OrderingRule[] hard =
        [
            OrderingRule.RestBeforeLast, OrderingRule.RestOrShopAdjacent, OrderingRule.TreasureRunCap,
            OrderingRule.EliteGap, OrderingRule.OpeningBan, OrderingRule.ElitesApart,
        ];

        for (var seed = 1; seed <= 400; seed++)
        {
            var mix = TestMixes.Small(new FixedRng(seed));
            var result = PathOrderer.Order(mix, new FixedRng(seed + 1000));
            var message = $"seed {seed} mix {mix}: {Letters(result.Path)} step {result.Step}";

            var valid = Permutations(mix)
                .Where(p => RuleValidator.EndsHold(p, mix))
                .Select(RuleValidator.Broken)
                .ToList();

            var lowest = valid.Min(broken => broken.Count == 0 ? 1 : Math.Max(1, broken.Max(RuleValidator.StepOf)));
            Assert.True(lowest == result.Step, $"{message}, brute force lowest step {lowest}");

            var kept = hard.Where(r => !result.BrokenRules.Contains(r)).ToList();
            foreach (var rule in result.BrokenRules.Where(hard.Contains))
                Assert.False(valid.Any(broken => !broken.Contains(rule) && !kept.Any(broken.Contains)),
                    $"{message}: {rule} could have been kept");
        }
    }

    [Fact]
    public void VanillaMixesKeepTheSpreadAndFightCap()
    {
        foreach (var (name, make) in TestMixes.Vanilla())
        {
            var mixRng = new FixedRng(name.GetHashCodeStable());
            int spread = 0, capped = 0, runs = 0;
            for (var seed = 1; seed <= SeedsPerCase; seed++)
            {
                var result = PathOrderer.Order(make(mixRng), new FixedRng(seed));
                if (result.Step == 0)
                    spread++;
                if (RuleValidator.MaxRun(result.Path, Monster, Elite) <= 4)
                    capped++;
                if (RuleValidator.MaxRun(result.Path, Monster) <= 3 && RuleValidator.MaxRun(result.Path, Unknown) <= 3)
                    runs++;
            }

            Assert.True(spread >= SeedsPerCase * 0.99, $"{name}: spread kept on {spread}/{SeedsPerCase}");
            Assert.True(capped >= SeedsPerCase * 0.99, $"{name}: fight cap met on {capped}/{SeedsPerCase}");
            Assert.True(runs >= SeedsPerCase * 0.99, $"{name}: run caps met on {runs}/{SeedsPerCase}");
        }
    }

    [Theory]
    [InlineData(1, 20, 5, 12, 3, 8, 3, "MUMUMTMERSMUURUMEUMMMRMUSMREMUMMRUEMMUUTRSMUTMERMMR")]
    [InlineData(2, 18, 8, 11, 3, 7, 4, "MMUMMUEURSMUUETTREUUMMUMMERMUSERMMMEURMTSMTERMUEMMR")]
    [InlineData(3, 2, 22, 10, 3, 7, 3, "MUEUSEUEREMEUEUERESETEREUETEUEREUERESEUETEREUER")]
    public void PinnedPaths(int seed, int monsters, int elites, int unknowns, int shops, int rests, int treasures,
        string expected)
    {
        var mix = new PointMix(monsters, elites, unknowns, shops, rests, treasures);
        Assert.Equal(expected, Letters(PathOrderer.Order(mix, new FixedRng(seed)).Path));
    }

    private static IEnumerable<PointType[]> Permutations(PointMix mix)
    {
        int[] counts = [mix.Monsters, mix.Elites, mix.Unknowns, mix.Shops, mix.RestSites, mix.Treasures];
        var slots = new PointType[mix.Total];
        return Fill(0);

        IEnumerable<PointType[]> Fill(int index)
        {
            if (index == slots.Length)
            {
                yield return (PointType[])slots.Clone();
                yield break;
            }

            for (var type = 0; type < counts.Length; type++)
            {
                if (counts[type] == 0)
                    continue;

                counts[type]--;
                slots[index] = (PointType)type;
                foreach (var permutation in Fill(index + 1))
                    yield return permutation;
                counts[type]++;
            }
        }
    }
}

internal static class StableHash
{
    public static int GetHashCodeStable(this string text)
    {
        var hash = 0;
        for (var i = 0; i < text.Length; i++)
            hash += text[i] * (i + 1);
        return hash;
    }
}
