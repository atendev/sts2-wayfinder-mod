namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

internal readonly record struct CountRange(int Min, int Max)
{
    public int Roll(IOrderingRng rng) => Min + rng.NextInt(Max - Min + 1);
}

/// <summary>An act's rolled mix of points and the placement flavor that goes with it.</summary>
internal sealed record ActLayout(PointMix Mix, PlacementRules Rules);

/// <summary>One act's count range for every point type and its length. Monsters fill whatever the length leaves.</summary>
internal sealed record ActPlan(
    string Name,
    CountRange Length,
    CountRange Elites,
    CountRange Unknowns,
    CountRange RestSites,
    CountRange Shops,
    CountRange Treasures,
    PlacementRules Flavor,
    bool RollsElitePairs = false)
{
    /// <summary>The most points that can be a fight: the longest act with every other type at its minimum. Unknowns count, because one can roll a fight.</summary>
    public int MaxFightPoints => Length.Max - Elites.Min - RestSites.Min - Shops.Min - Treasures.Min;

    public ActLayout Roll(IOrderingRng rng)
    {
        var length = Length.Roll(rng);
        var elites = Elites.Roll(rng);
        var unknowns = Unknowns.Roll(rng);
        var restSites = RestSites.Roll(rng);
        var shops = Shops.Roll(rng);
        var treasures = Treasures.Roll(rng);

        var mix = new PointMix(length - elites - unknowns - restSites - shops - treasures, elites, unknowns, shops, restSites, treasures);
        if (mix.Monsters < 1)
            throw new InvalidOperationException($"Act plan {Name} rolled a mix with no Monsters: {mix}");

        // Every pair needs a rest site of its own right before it, and the last rest site stays before the boss.
        var maxPairs = Math.Min(elites / 2, restSites - 1);
        var pairs = RollsElitePairs && maxPairs > 0 ? 1 + rng.NextInt(maxPairs) : 0;
        return new ActLayout(mix, Flavor with { ElitePairs = pairs });
    }
}

/// <summary>The per-act ranges for Wayfinder's Acts path length. Names are for tuning and the mod description and never shown in game.</summary>
internal static class ActPlans
{
    /// <summary>Act 1: fights and card rewards drive growth, Elites come later and spread out. The Underdocks take the same slot (act index 0), so they use this plan.</summary>
    public static readonly ActPlan BuildTheDeck = new("Build the deck",
        Length: new(34, 38), Elites: new(3, 4), Unknowns: new(3, 5), RestSites: new(5, 6), Shops: new(2, 2), Treasures: new(2, 2),
        Flavor: new PlacementRules(NoEliteInFirstThird: true, EliteDistance: 6));

    /// <summary>Act 2: the most Unknowns, which may come in runs of 2 to 3 under the shared run cap, and the most Shops.</summary>
    public static readonly ActPlan TheWilds = new("The wilds",
        Length: new(32, 36), Elites: new(3, 5), Unknowns: new(8, 12), RestSites: new(4, 5), Shops: new(3, 3), Treasures: new(1, 2),
        Flavor: PlacementRules.Standard);

    /// <summary>Act 3: shortest and most Elite-dense, with Elite pairs.</summary>
    public static readonly ActPlan TheGauntlet = new("The gauntlet",
        Length: new(26, 30), Elites: new(4, 6), Unknowns: new(2, 4), RestSites: new(3, 4), Shops: new(1, 1), Treasures: new(1, 1),
        Flavor: PlacementRules.Standard, RollsElitePairs: true);

    private static readonly ActPlan[] All = [BuildTheDeck, TheWilds, TheGauntlet];

    /// <summary>The plan for a 0-based act index. A modded run with more acts reuses the last plan.</summary>
    public static ActPlan For(int actIndex) => All[Math.Clamp(actIndex, 0, All.Length - 1)];
}
