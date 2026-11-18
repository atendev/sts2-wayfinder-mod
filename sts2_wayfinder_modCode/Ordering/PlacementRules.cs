namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

/// <summary>
/// The per-act placement flavor on top of the shared ordering rules. It changes how the Elite rules read, so each
/// still gives way at its own step of the break order.
/// </summary>
/// <param name="NoEliteInFirstThird">No Elite in the first third of the path. Joins the opening ban (step 6).</param>
/// <param name="EliteDistance">The fewest positions between two Elites. 3 means two points between. Replaces the gap in the Elite gap rule (step 5).</param>
/// <param name="ElitePairs">0 allows no side by side Elites. Above 0, two Elites may sit side by side as a pair and the spread places this many. A pair needs a rest site right before it and never grows to three, which is part of the Elites apart rule (step 7). Pair clearance is part of the Elite gap rule (step 5).</param>
internal sealed record PlacementRules(bool NoEliteInFirstThird = false, int EliteDistance = 3, int ElitePairs = 0)
{
    /// <summary>The fewest points between a pair and any other Elite, on either side.</summary>
    public const int PairClearance = 4;

    /// <summary>The shared rules with no per-act flavor, as v0.1 ordered every act.</summary>
    public static readonly PlacementRules Standard = new();

    public bool PairsAllowed => ElitePairs > 0;

    /// <summary>The first 1-based position an Elite may take under the first-third ban.</summary>
    public int FirstEliteAfterThird(int pathLength) => NoEliteInFirstThird ? pathLength / 3 + 1 : 1;

    /// <summary>How far the "points since the last Elite" counter has to count before it stops mattering.</summary>
    public int GapCap => Math.Max(EliteDistance - 1, PairsAllowed ? PairClearance : 0);
}
