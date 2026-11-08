namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

/// <summary>Turns an act's mix of points into one ordered path. Never fails and places every point.</summary>
internal static class PathOrderer
{
    private const int MaxFightRun = 4;
    private const int LastStep = 7;

    private static readonly OrderingRule[] RestoreOrder =
    [
        OrderingRule.ElitesApart,
        OrderingRule.OpeningBan,
        OrderingRule.EliteGap,
        OrderingRule.TreasureRunCap,
        OrderingRule.RestOrShopAdjacent,
        OrderingRule.RestBeforeLast,
    ];

    public static OrderingResult Order(PointMix mix, IOrderingRng rng)
    {
        var spread = SpreadPlacer.TryPlace(mix, rng, MaxFightRun) ?? SpreadPlacer.TryPlace(mix, rng, null);
        if (spread != null)
            return new OrderingResult(spread, 0, []);

        for (var step = 1; step <= LastStep; step++)
        {
            var rules = HardRules.AtStep(step);
            var path = HardRuleSearch.TryOrder(mix, rules, rng);
            if (path == null)
                continue;

            foreach (var rule in RestoreOrder)
            {
                if (rules.Keeps(rule))
                    continue;

                var trial = rules.With(rule);
                var restored = HardRuleSearch.TryOrder(mix, trial, rng);
                if (restored == null)
                    continue;

                rules = trial;
                path = restored;
            }

            return new OrderingResult(path, step, rules.Broken());
        }

        throw new InvalidOperationException($"No order found for {mix}");
    }
}

internal readonly record struct HardRules(int KeptMask)
{
    public static HardRules AtStep(int step)
    {
        var mask = 0;
        foreach (var rule in Enum.GetValues<OrderingRule>())
            if ((int)rule > step)
                mask |= Bit(rule);

        return new HardRules(mask);
    }

    public bool Keeps(OrderingRule rule) => (KeptMask & Bit(rule)) != 0;

    public HardRules With(OrderingRule rule) => new(KeptMask | Bit(rule));

    public IReadOnlyList<OrderingRule> Broken()
    {
        var mask = KeptMask;
        return Enum.GetValues<OrderingRule>().Where(r => (mask & Bit(r)) == 0).ToList();
    }

    private static int Bit(OrderingRule rule) => 1 << (int)rule;
}
