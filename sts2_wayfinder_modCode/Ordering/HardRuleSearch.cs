namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

internal sealed class HardRuleSearch
{
    private const int NodeBudget = 50_000;
    private const int OpeningLength = 5;
    private const int TreasureRunCap = 3;
    private const int NoEliteYet = -1;

    private static readonly PointType[] SlotOrder =
        [PointType.Elite, PointType.RestSite, PointType.Shop, PointType.Treasure, PointType.Monster];

    private readonly PointMix _mix;
    private readonly HardRules _rules;
    private readonly PlacementRules _placement;
    private readonly IOrderingRng _rng;
    private readonly int _n;
    private readonly bool _fixedFirst;
    private readonly bool _fixedLast;
    private readonly int[] _counts;
    private readonly PointType[] _slots;
    private readonly HashSet<SearchKey> _dead = [];
    private int _nodes;

    /// <param name="Since">Points since the last Elite, counted up to the gap cap.</param>
    /// <param name="LastGap">Points between the last Elite and the one before it, counted up to the pair clearance. Zero when there is no earlier Elite.</param>
    /// <param name="LastWasPair">The last Elite is the second of a pair.</param>
    /// <param name="HadRestBefore">The point right before the last Elite's group was a rest site.</param>
    private readonly record struct EliteState(int Since, int LastGap, bool LastWasPair, bool HadRestBefore)
    {
        public static readonly EliteState None = new(NoEliteYet, 0, false, false);
    }

    private readonly record struct SearchKey(int Pos, int Elites, int Rests, int Shops, int Treasures, int Fights, int Previous, EliteState Elite, int TreasureRun);

    private HardRuleSearch(PointMix mix, HardRules rules, PlacementRules placement, IOrderingRng rng)
    {
        _mix = mix;
        _rules = rules;
        _placement = placement;
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

    public static PointType[]? TryOrder(PointMix mix, HardRules rules, IOrderingRng rng, PlacementRules? placement = null)
    {
        var search = new HardRuleSearch(mix, rules, placement ?? PlacementRules.Standard, rng);
        return search.Place(1, null, EliteState.None, 0) ? search.BuildPath() : null;
    }

    private bool Place(int pos, PointType? previous, EliteState elite, int treasureRun)
    {
        if (pos > _n)
            return true;
        if (++_nodes > NodeBudget)
            return false;

        var key = new SearchKey(pos, _counts[0], _counts[1], _counts[2], _counts[3], _counts[4],
            previous == null ? -1 : (int)previous, elite, treasureRun);
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
            if (!Allowed(pos, slot, previous, elite, treasureRun))
                continue;

            if (!isFixed)
                _counts[Array.IndexOf(SlotOrder, slot)]--;

            _slots[pos] = slot;
            if (Place(pos + 1, slot, NextElite(pos, slot, previous, elite), slot == PointType.Treasure ? treasureRun + 1 : 0))
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

    private bool Allowed(int pos, PointType slot, PointType? previous, EliteState elite, int treasureRun)
    {
        if (_rules.Keeps(OrderingRule.OpeningBan) && pos <= OpeningLength && !IsFixed(pos)
            && slot is PointType.Elite or PointType.RestSite or PointType.Treasure)
            return false;

        if (slot == PointType.Elite && _rules.Keeps(OrderingRule.OpeningBan) && pos < _placement.FirstEliteAfterThird(_n))
            return false;

        if (_rules.Keeps(OrderingRule.RestBeforeLast) && slot == PointType.RestSite && (pos == _n - 2 || pos == _n - 1))
            return false;

        if (_rules.Keeps(OrderingRule.RestOrShopAdjacent) && slot == previous && slot is PointType.RestSite or PointType.Shop)
            return false;

        if (slot == PointType.Elite && !EliteAllowed(previous, elite))
            return false;

        return slot != PointType.Treasure || !_rules.Keeps(OrderingRule.TreasureRunCap) || treasureRun < TreasureRunCap;
    }

    private bool EliteAllowed(PointType? previous, EliteState elite)
    {
        var keepsApart = _rules.Keeps(OrderingRule.ElitesApart);
        var keepsGap = _rules.Keeps(OrderingRule.EliteGap);

        if (previous == PointType.Elite)
        {
            if (!keepsApart && !keepsGap)
                return true;
            if (!_placement.PairsAllowed)
                return false;

            // A pair has a rest site right before it, never grows to three, and keeps its clearance from the Elite before it.
            if (keepsApart && (!elite.HadRestBefore || elite.LastWasPair))
                return false;

            return !keepsGap || elite.LastGap == 0 || elite.LastGap >= PlacementRules.PairClearance;
        }

        if (!keepsGap || elite.Since == NoEliteYet)
            return true;

        var needed = elite.LastWasPair ? PlacementRules.PairClearance : _placement.EliteDistance - 1;
        return elite.Since >= needed;
    }

    private EliteState NextElite(int pos, PointType slot, PointType? previous, EliteState elite)
    {
        if (slot != PointType.Elite)
        {
            if (elite.Since == NoEliteYet)
                return elite;

            var since = Math.Min(elite.Since + 1, _placement.GapCap);
            return since >= _placement.GapCap ? new EliteState(since, 0, false, false) : elite with { Since = since };
        }

        if (previous == PointType.Elite)
            return elite with { Since = 0, LastWasPair = true };

        var lastGap = elite.Since == NoEliteYet ? 0 : Math.Min(elite.Since, PlacementRules.PairClearance);
        var restBefore = _placement.PairsAllowed && previous == PointType.RestSite;
        return new EliteState(0, lastGap, false, restBefore);
    }

    private bool CannotFit(int pos)
    {
        var lastFree = _fixedLast ? _n - 1 : _n;
        var earliest = _rules.Keeps(OrderingRule.OpeningBan) ? Math.Max(pos, OpeningLength + 1) : pos;
        var eliteEarliest = _rules.Keeps(OrderingRule.OpeningBan) ? Math.Max(earliest, _placement.FirstEliteAfterThird(_n)) : earliest;

        // Pairs let Elites sit side by side, so only the plain spacing rules bound the count.
        var eliteSpacing = _placement.PairsAllowed ? 1
            : _rules.Keeps(OrderingRule.EliteGap) ? _placement.EliteDistance
            : _rules.Keeps(OrderingRule.ElitesApart) ? 2
            : 1;
        if (eliteSpacing > 1 && !Fits(_counts[0], eliteEarliest, lastFree, eliteSpacing))
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
