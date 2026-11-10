namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal enum EliteRelicTier
{
    Common,
    Uncommon,
    Rare,
}

internal static class EliteRelicRarity
{
    /// <summary>Vanilla's relic rarity roll is under 0.5 Common and under 0.83 Uncommon, so 50/33/17. Lowered Elite relics are 75/20/5.</summary>
    public const float CommonBelow = 0.75f;
    public const float UncommonBelow = 0.95f;

    public static EliteRelicTier Tier(float roll) =>
        roll < CommonBelow ? EliteRelicTier.Common : roll < UncommonBelow ? EliteRelicTier.Uncommon : EliteRelicTier.Rare;
}
