using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace OrderingTests;

// Modeled from .plan/maps/v0-1-0/assets/08-preview-sample-paths.py. Not measured in game.
internal static class TestMixes
{
    private sealed record Act(int Rooms, int RestLow, int RestHigh, int UnknownLow, int UnknownHigh);

    private static readonly Act[] Acts =
    [
        new(15, 6, 7, 10, 14),
        new(14, 6, 7, 9, 13),
        new(13, 5, 6, 9, 13),
    ];

    private static readonly int[] WidthWeights = [2, 8, 16, 24, 24, 16, 10];

    public static IEnumerable<(string Name, Func<FixedRng, PointMix> Make)> Vanilla()
    {
        for (var a = 0; a < Acts.Length; a++)
        {
            var act = Acts[a];
            yield return ($"act {a + 1}, 5 elites", rng => ActMix(rng, act, 5, false));
            yield return ($"act {a + 1}, 8 elites", rng => ActMix(rng, act, 8, false));
        }
    }

    public static IEnumerable<(string Name, Func<FixedRng, PointMix> Make)> Stressed()
    {
        for (var a = 0; a < Acts.Length; a++)
        {
            var act = Acts[a];
            yield return ($"act {a + 1}, BigGameHunter", rng => ActMix(rng, act, 5, true));
        }

        yield return ("elite heavy", EliteHeavy);
        yield return ("single type", SingleType);
    }

    public static readonly (string Name, PointMix Mix)[] Edge =
    [
        ("empty", default),
        ("one monster", new PointMix(Monsters: 1)),
        ("one rest site", new PointMix(RestSites: 1)),
        ("no monster", new PointMix(Elites: 3, Unknowns: 8, Shops: 2, RestSites: 5, Treasures: 4)),
        ("no rest site", new PointMix(Monsters: 12, Elites: 3, Unknowns: 8, Shops: 2, Treasures: 4)),
        ("under 6 points", new PointMix(Monsters: 2, Elites: 1, RestSites: 1, Treasures: 1)),
        ("all rest sites", new PointMix(RestSites: 9)),
        ("all elites", new PointMix(Elites: 9)),
    ];

    public static PointMix Small(FixedRng rng)
    {
        var weights = new int[6];
        for (var i = 0; i < weights.Length; i++)
            weights[i] = 1 + rng.NextInt(8);

        var counts = new int[6];
        var size = 1 + rng.NextInt(7);
        for (var i = 0; i < size; i++)
            counts[Pick(rng, weights)]++;

        return FromCounts(counts);
    }

    private static PointMix ActMix(FixedRng rng, Act act, int elites, bool hunter)
    {
        while (true)
        {
            var widths = new int[act.Rooms];
            for (var i = 0; i < widths.Length; i++)
                widths[i] = 1 + Pick(rng, WidthWeights);

            int first = Math.Max(2, widths[0]), treasure = widths[act.Rooms - 7], last = widths[^1];
            var kept = Math.Max(widths.Sum() * (80 + rng.NextInt(21)) / 100, first + treasure + last);
            var rest = last + Between(rng, act.RestLow, act.RestHigh) - (rng.NextInt(5) == 0 ? 1 + rng.NextInt(2) : 0);
            var elite = Math.Max(0, elites - (rng.NextInt(5) == 0 ? 1 : 0));
            var shop = Math.Max(0, 3 - (rng.NextInt(10) == 0 ? 1 : 0));
            var unknown = Between(rng, act.UnknownLow, act.UnknownHigh);
            var monster = kept - rest - elite - shop - unknown - treasure;
            if (monster < first)
                continue;

            if (hunter)
            {
                elite += monster - first;
                monster = first;
            }

            return new PointMix(monster, elite, unknown, shop, rest, treasure);
        }
    }

    private static PointMix EliteHeavy(FixedRng rng)
    {
        var size = Between(rng, 30, 90);
        var elites = size * Between(rng, 30, 75) / 100;
        var counts = new int[6];
        counts[1] = elites;
        int[] others = [0, 2, 3, 4, 5];
        int[] weights = [6, 3, 1, 3, 1];
        for (var i = 0; i < size - elites; i++)
            counts[others[Pick(rng, weights)]]++;

        return FromCounts(counts);
    }

    private static PointMix SingleType(FixedRng rng)
    {
        var counts = new int[6];
        counts[rng.NextInt(6)] = Between(rng, 1, 80);
        return FromCounts(counts);
    }

    private static PointMix FromCounts(int[] c) => new(c[0], c[1], c[2], c[3], c[4], c[5]);

    private static int Between(FixedRng rng, int low, int high) => low + rng.NextInt(high - low + 1);

    private static int Pick(FixedRng rng, int[] weights)
    {
        var roll = rng.NextInt(weights.Sum());
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0)
                return i;
        }

        throw new InvalidOperationException();
    }
}
