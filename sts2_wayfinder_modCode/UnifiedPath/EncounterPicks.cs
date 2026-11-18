using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>
/// Builds an act's encounter lists so picks stay random after the vanilla list runs out. Vanilla indexes each list with a
/// visit counter modulo its length, so a long act replays the list in the same order. The lists are written once at
/// generation and ride in the run save, so the picks are the same after a save and reload.
/// </summary>
internal static class EncounterPicks
{
    /// <summary>Long enough that an act on the Full map path never reaches the wrap. The vanilla list is 15 and a Full map act has at most about 30 Elites.</summary>
    public const int EliteListLength = 48;

    /// <summary>Vanilla's normal list is 13 to 15, doubled by the long path. A Full map act has about 35 normal fights at most.</summary>
    public const int NormalListLength = 96;

    /// <summary>Each pool Elite once in a fresh seeded shuffle, then random picks that skip the Elite just fought. The vanilla list is only the fallback for an empty pool.</summary>
    public static List<T> BuildEliteList<T>(IReadOnlyList<T> vanilla, IReadOnlyList<T> pool, int length, IOrderingRng rng) where T : class
    {
        var distinct = pool.Distinct().ToList();
        if (distinct.Count == 0)
            return vanilla.ToList();

        var opening = distinct.ToList();
        Shuffle(opening, rng);

        var list = opening.Take(length).ToList();
        while (list.Count < length)
            list.Add(PickExcludingLast(distinct, list[^1], rng));

        return list;
    }

    /// <summary>The vanilla list unchanged, then random regular encounters. A weak encounter never comes back, because the pool holds none.</summary>
    public static List<T> ExtendNormalList<T>(IReadOnlyList<T> vanilla, IReadOnlyList<T> regularPool, int length, IOrderingRng rng) where T : class
    {
        var distinct = regularPool.Distinct().ToList();
        var list = vanilla.ToList();
        if (distinct.Count == 0)
            return list;

        while (list.Count < length)
            list.Add(PickExcludingLast(distinct, list.Count > 0 ? list[^1] : null, rng));

        return list;
    }

    private static T PickExcludingLast<T>(IReadOnlyList<T> pool, T? last, IOrderingRng rng) where T : class
    {
        if (pool.Count == 1)
            return pool[0];

        var candidates = pool.Where(item => !ReferenceEquals(item, last)).ToList();
        return candidates[rng.NextInt(candidates.Count)];
    }

    private static void Shuffle<T>(List<T> items, IOrderingRng rng)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = rng.NextInt(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
