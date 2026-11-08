using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;
using static sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering.PointType;

namespace OrderingTests;

// Written from the text of the ordering decision, separately from the engine's own checks.
internal static class RuleValidator
{
    public static bool EndsHold(IReadOnlyList<PointType> path, PointMix mix) =>
        (mix.Monsters == 0 || path[0] == Monster) && (mix.RestSites == 0 || path[^1] == RestSite);

    public static HashSet<OrderingRule> Broken(IReadOnlyList<PointType> path)
    {
        var broken = new HashSet<OrderingRule>();
        var n = path.Count;

        for (var i = 0; i < Math.Min(5, n); i++)
        {
            var exemptLastRest = i == n - 1 && path[i] == RestSite;
            if (path[i] is Elite or RestSite or Treasure && !exemptLastRest)
                broken.Add(OrderingRule.OpeningBan);
        }

        for (var i = Math.Max(0, n - 3); i < n - 1; i++)
            if (path[i] == RestSite)
                broken.Add(OrderingRule.RestBeforeLast);

        for (var i = 0; i + 1 < n; i++)
            if (path[i] == path[i + 1] && path[i] is RestSite or Shop)
                broken.Add(OrderingRule.RestOrShopAdjacent);

        if (MaxRun(path, Treasure) > 3)
            broken.Add(OrderingRule.TreasureRunCap);

        var elites = Enumerable.Range(0, n).Where(i => path[i] == Elite).ToList();
        for (var i = 0; i + 1 < elites.Count; i++)
        {
            var between = elites[i + 1] - elites[i] - 1;
            if (between < 2)
                broken.Add(OrderingRule.EliteGap);
            if (between == 0)
                broken.Add(OrderingRule.ElitesApart);
        }

        return broken;
    }

    public static int MaxRun(IReadOnlyList<PointType> path, params PointType[] types)
    {
        int best = 0, run = 0;
        foreach (var point in path)
        {
            run = types.Contains(point) ? run + 1 : 0;
            best = Math.Max(best, run);
        }

        return best;
    }

    public static int StepOf(OrderingRule rule) => rule switch
    {
        OrderingRule.Preferences => 1,
        OrderingRule.RestBeforeLast => 2,
        OrderingRule.RestOrShopAdjacent => 3,
        OrderingRule.TreasureRunCap => 4,
        OrderingRule.EliteGap => 5,
        OrderingRule.OpeningBan => 6,
        OrderingRule.ElitesApart => 7,
        _ => throw new ArgumentOutOfRangeException(nameof(rule)),
    };
}
