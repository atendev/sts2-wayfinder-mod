namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal readonly record struct EnemyScalingToggles(bool ScaleBosses, bool ScalePathHp, bool ScalePathDamage)
{
    public static readonly EnemyScalingToggles Off = new(false, false, false);
    public static readonly EnemyScalingToggles PowerCurve = new(true, true, true);
}

internal static class DifficultySettings
{
    public static EnemyScalingToggles EnemyScaling { get; set; } = EnemyScalingToggles.PowerCurve;

    public static TreasureOdds Treasure { get; set; } = TreasureOdds.Balanced;
}
