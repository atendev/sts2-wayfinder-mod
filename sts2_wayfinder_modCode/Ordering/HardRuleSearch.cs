namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

internal sealed class HardRuleSearch
{
    private const int NodeBudget = 50_000;
    private const int OpeningLength = 5;
    private const int EliteGap = 2;
    private const int TreasureRunCap = 3;
    private const int NoEliteYet = -1;

    private static readonly PointType[] SlotOrder =
        [PointType.Elite, PointType.RestSite, PointType.Shop, PointType.Treasure, PointType.Monster];

    private readonly PointMix _mix;
    private readonly HardRules _rules;
    private readonly IOrderingRng _rng;
    private readonly int _n;
    private readonly bool _fixedFirst;
    private readonly bool _fixedLast;
    private readonly int[] _counts;
    private readonly PointType[] _slots;
    private readonly HashSet<(int, int, int, int, int, int, int, int, int)> _dead = [];
    private int _nodes;

    private HardRuleSearch(PointMix mix, HardRules rules, IOrderingRng rng)
    {
        _mix = mix;
        _rules = rules;
        _rng = rng;
        _n = mix.Total;
        _fixedFirst = mix.Monsters > 0;
        _fixedLast = mix.RestSites > 0;
        _counts =
        [
            mix.Elites,
            mix.RestSites - (_fixedLast ? 1 : 0),
            mix.Shops,
            mix.Treasures,
            mix.Monsters + mix.Unknowns - (_fixedFirst ? 1 : 0),
        ];
        _slots = new PointType[_n + 1];
    }

    public static PointType[]? TryOrder(PointMix mix, HardRules rules, IOrderingRng rng)
    {
        var search = new HardRuleSearch(mix, rules, rng);
        return search.Place(1, null, NoEliteYet, 0) ? search.BuildPath() : null;
    }

    private bool Place(int pos, PointType? previous, int sinceElite, int treasureRun)
    {
        if (pos > _n)
            return true;
        if (++_nodes > NodeBudget)
            return false;

        var key = (pos, _counts[0], _counts[1], _counts[2], _counts[3], _counts[4],
            previous == null ? -1 : (int)previous, sinceElite, treasureRun);
        if (_dead.Contains(key))
            return false;

        if (CannotFit(pos))
        {
            _dead.Add(key);
            return false;
        }

        var isFixed = IsFixed(pos);
        foreach (var slot in Options(pos))
        {
            if (!Allowed(pos, slot, previous, sinceElite, treasureRun))
                continue;

            if (!isFixed)
                _counts[Array.IndexOf(SlotOrder, slot)]--;

            _slots[pos] = slot;
            var nextSince = slot == PointType.Elite ? 0
                : sinceElite == NoEliteYet ? NoEliteYet
                : Math.Min(sinceElite + 1, EliteGap);

            if (Place(pos + 1, slot, nextSince, slot == PointType.Treasure ? treasureRun + 1 : 0))
                return true;

            if (!isFixed)
                _counts[Array.IndexOf(SlotOrder, slot)]++;
        }

        if (_nodes <= NodeBudget)
            _dead.Add(key);
        return false;
    }

    private bool IsFixed(int pos) => (pos == 1 && _fixedFirst) || (pos == _n && _fixedLast);

    private PointType[] Options(int pos)
    {
        if (pos == 1 && _fixedFirst)
            return [PointType.Monster];
        if (pos == _n && _fixedLast)
            return [PointType.RestSite];

        var remaining = (int[])_counts.Clone();
        var total = remaining.Sum();
        var options = new List<PointType>(SlotOrder.Length);
        while (total > 0)
        {
            var roll = _rng.NextInt(total);
            var index = 0;
            while (roll >= remaining[index])
                roll -= remaining[index++];

            options.Add(SlotOrder[index]);
            total -= remaining[index];
            remaining[index] = 0;
        }

        return options.ToArray();
    }

    private bool Allowed(int pos, PointType slot, PointType? previous, int sinceElite, int treasureRun)
    {
        if (_rules.Keeps(OrderingRule.OpeningBan) && pos <= OpeningLength && !IsFixed(pos)
            && slot is PointType.Elite or PointType.RestSite or PointType.Treasure)
            return false;

        if (_rules.Keeps(OrderingRule.RestBeforeLast) && slot == PointType.RestSite && (pos == _n - 2 || pos == _n - 1))
            return false;

        if (_rules.Keeps(OrderingRule.RestOrShopAdjacent) && slot == previous && slot is PointType.RestSite or PointType.Shop)
            return false;

        if (slot == PointType.Elite && previous == PointType.Elite && _rules.Keeps(OrderingRule.ElitesApart))
            return false;

        if (slot == PointType.Elite && _rules.Keeps(OrderingRule.EliteGap) && sinceElite != NoEliteYet && sinceElite < EliteGap)
            return false;

        return slot != PointType.Treasure || !_rules.Keeps(OrderingRule.TreasureRunCap) || treasureRun < TreasureRunCap;
    }

    private bool CannotFit(int pos)
    {
        var lastFree = _fixedLast ? _n - 1 : _n;
        var earliest = _rules.Keeps(OrderingRule.OpeningBan) ? Math.Max(pos, OpeningLength + 1) : pos;

        var eliteSpacing = _rules.Keeps(OrderingRule.EliteGap) ? EliteGap + 1
            : _rules.Keeps(OrderingRule.ElitesApart) ? 2
            : 1;
        if (eliteSpacing > 1 && !Fits(_counts[0], earliest, lastFree, eliteSpacing))
            return true;

        if (!_rules.Keeps(OrderingRule.RestOrShopAdjacent))
            return false;

        var restEnd = !_fixedLast ? lastFree
            : _rules.Keeps(OrderingRule.RestBeforeLast) ? lastFree - 2
            : lastFree - 1;
        return !Fits(_counts[1], earliest, restEnd, 2) || !Fits(_counts[2], pos, lastFree, 2);
    }

    private static bool Fits(int count, int low, int high, int spacing) =>
        count == 0 || (count - 1) * spacing + 1 <= high - low + 1;

    private PointType[] BuildPath()
    {
        var filler = Enumerable.Repeat(PointType.Monster, _mix.Monsters - (_fixedFirst ? 1 : 0))
            .Concat(Enumerable.Repeat(PointType.Unknown, _mix.Unknowns))
            .ToArray();
        RandomOrder.Shuffle(filler, _rng);

        var path = new PointType[_n];
        var next = 0;
        for (var pos = 1; pos <= _n; pos++)
        {
            if (_slots[pos] != PointType.Monster)
                path[pos - 1] = _slots[pos];
            else
                path[pos - 1] = pos == 1 && _fixedFirst ? PointType.Monster : filler[next++];
        }

        return path;
    }
}
