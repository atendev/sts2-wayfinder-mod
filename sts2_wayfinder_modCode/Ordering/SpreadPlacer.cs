namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

internal static class SpreadPlacer
{
    private const int MinPoints = 8;
    private const int PlaceAttempts = 30;
    private const int ShuffleAttempts = 40;
    private const int OpeningEnd = 5;
    private const int JitterScale = 1000;
    private const int JitterRange = 300;
    private const int MaxRunOfOneType = 3;

    public static PointType[]? TryPlace(PointMix mix, IOrderingRng rng, int? fightCap)
    {
        var n = mix.Total;
        if (mix.Monsters == 0 || mix.RestSites == 0 || n < MinPoints)
            return null;

        for (var attempt = 0; attempt < PlaceAttempts; attempt++)
        {
            var slots = new PointType?[n];
            slots[0] = PointType.Monster;
            slots[n - 1] = PointType.RestSite;

            if (!Spread(slots, PointType.Elite, 6, n - 1, mix.Elites, rng)
                || !Spread(slots, PointType.RestSite, 6, n - 3, mix.RestSites - 1, rng)
                || !Spread(slots, PointType.Shop, 2, n - 1, mix.Shops, rng)
                || !PlaceTreasure(slots, mix.Treasures, rng))
                continue;

            var filled = FillWithShuffledFights(slots, mix, rng, fightCap);
            if (filled != null)
                return filled;
        }

        return null;
    }

    private static bool Spread(PointType?[] slots, PointType type, int low, int high, int count, IOrderingRng rng)
    {
        if (count == 0)
            return true;
        if (high < low)
            return false;

        var span = high - low + 1;
        for (var i = 0; i < count; i++)
        {
            var jitter = rng.NextInt(2 * JitterRange + 1) - JitterRange;
            var centre = (long)(low - 1) * JitterScale * count + (long)span * (JitterScale * i + JitterScale / 2 + jitter);
            var nearest = Enumerable.Range(low - 1, span)
                .OrderBy(pos => Math.Abs((long)pos * JitterScale * count - centre))
                .Where(pos => CanPlace(slots, type, pos))
                .Select(pos => (int?)pos)
                .FirstOrDefault();

            if (nearest == null)
                return false;

            slots[nearest.Value] = type;
        }

        return true;
    }

    private static bool CanPlace(PointType?[] slots, PointType type, int pos)
    {
        if (slots[pos] != null || At(slots, pos - 1) == type || At(slots, pos + 1) == type)
            return false;

        return type != PointType.Elite || (At(slots, pos - 2) != type && At(slots, pos + 2) != type);
    }

    private static PointType? At(PointType?[] slots, int pos) =>
        pos >= 0 && pos < slots.Length ? slots[pos] : null;

    private static bool PlaceTreasure(PointType?[] slots, int count, IOrderingRng rng)
    {
        var free = Enumerable.Range(OpeningEnd, slots.Length - 1 - OpeningEnd).Where(i => slots[i] == null).ToList();
        if (free.Count < count)
            return false;

        for (var i = 0; i < count; i++)
        {
            var pick = i + rng.NextInt(free.Count - i);
            (free[i], free[pick]) = (free[pick], free[i]);
            slots[free[i]] = PointType.Treasure;
        }

        return true;
    }

    private static PointType[]? FillWithShuffledFights(PointType?[] slots, PointMix mix, IOrderingRng rng, int? fightCap)
    {
        var filler = Enumerable.Repeat(PointType.Monster, mix.Monsters - 1)
            .Concat(Enumerable.Repeat(PointType.Unknown, mix.Unknowns))
            .ToArray();

        for (var attempt = 0; attempt < ShuffleAttempts; attempt++)
        {
            RandomOrder.Shuffle(filler, rng);
            var next = 0;
            var path = slots.Select(slot => slot ?? filler[next++]).ToArray();
            if (RunsAreCapped(path, fightCap))
                return path;
        }

        return null;
    }

    private static bool RunsAreCapped(PointType[] path, int? fightCap) =>
        MaxRun(path, t => t == PointType.Treasure) <= MaxRunOfOneType
        && MaxRun(path, t => t == PointType.Monster) <= MaxRunOfOneType
        && MaxRun(path, t => t == PointType.Unknown) <= MaxRunOfOneType
        && (fightCap == null || MaxRun(path, t => t is PointType.Monster or PointType.Elite) <= fightCap);

    private static int MaxRun(PointType[] path, Func<PointType, bool> matches)
    {
        int best = 0, run = 0;
        foreach (var point in path)
        {
            run = matches(point) ? run + 1 : 0;
            best = Math.Max(best, run);
        }

        return best;
    }
}

internal static class RandomOrder
{
    public static void Shuffle<T>(T[] items, IOrderingRng rng)
    {
        for (var i = items.Length - 1; i > 0; i--)
        {
            var pick = rng.NextInt(i + 1);
            (items[i], items[pick]) = (items[pick], items[i]);
        }
    }
}
