namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

internal enum PointType
{
    Monster,
    Elite,
    Unknown,
    Shop,
    RestSite,
    Treasure,
}

/// <summary>The points between an act's start point and its boss.</summary>
internal readonly record struct PointMix(
    int Monsters = 0,
    int Elites = 0,
    int Unknowns = 0,
    int Shops = 0,
    int RestSites = 0,
    int Treasures = 0)
{
    public int Total => Monsters + Elites + Unknowns + Shops + RestSites + Treasures;
}

/// <summary>The only source of randomness the engine uses, so a seed fully decides the path.</summary>
internal interface IOrderingRng
{
    /// <returns>A value from 0 up to, but not including, <paramref name="maxExclusive"/>.</returns>
    int NextInt(int maxExclusive);
}

/// <summary>The rules that can give way, numbered by their step in the break order.</summary>
internal enum OrderingRule
{
    Preferences = 1,
    RestBeforeLast = 2,
    RestOrShopAdjacent = 3,
    TreasureRunCap = 4,
    EliteGap = 5,
    OpeningBan = 6,
    ElitesApart = 7,
}

/// <param name="Path">Every point of the mix, in walking order.</param>
/// <param name="Step">The break-order step reached. 0 means every rule and the spread held.</param>
/// <param name="BrokenRules">The rules the path gave up after restoring all it could.</param>
internal sealed record OrderingResult(
    IReadOnlyList<PointType> Path,
    int Step,
    IReadOnlyList<OrderingRule> BrokenRules);
