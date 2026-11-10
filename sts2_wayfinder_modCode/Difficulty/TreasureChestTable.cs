namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>The chest odds behind "Use treasure chest chances": the first chest's relic chance, how much each gold chest adds, and the gold chest's gold multiplier.</summary>
internal readonly record struct TreasureOdds(bool Enabled, decimal StartChance, decimal Step, decimal GoldMultiplier)
{
    public static readonly TreasureOdds Off = new(false, 1m, 0m, 1m);
    public static readonly TreasureOdds Generous = new(true, 0.25m, 0.25m, 2m);
    public static readonly TreasureOdds Balanced = new(true, 0.20m, 0.20m, 2m);
    public static readonly TreasureOdds Stingy = new(true, 0.15m, 0.15m, 1.5m);
}

/// <summary>What an earlier chest in the act gave: <see cref="Empty"/> is a chest that gave neither relic nor gold, like Silver Crucible's.</summary>
internal enum ChestResult
{
    Relic,
    Gold,
    Empty,
}

internal static class TreasureChestTable
{
    /// <summary>The chance the next chest is a relic chest: the start chance plus one step per gold chest since the last relic chest.</summary>
    public static decimal RelicChance(TreasureOdds odds, IEnumerable<ChestResult> earlierInAct)
    {
        var misses = 0;
        foreach (var result in earlierInAct)
        {
            if (result == ChestResult.Relic)
                misses = 0;
            else if (result == ChestResult.Gold)
                misses++;
        }

        return Math.Min(1m, odds.StartChance + odds.Step * misses);
    }

    /// <param name="roll">A draw from 0 up to 1.</param>
    /// <param name="isLastChestOfAct">The act's last treasure point is always a relic chest.</param>
    public static bool IsRelicChest(TreasureOdds odds, IEnumerable<ChestResult> earlierInAct, bool isLastChestOfAct, float roll) =>
        isLastChestOfAct || (decimal)roll < RelicChance(odds, earlierInAct);
}
