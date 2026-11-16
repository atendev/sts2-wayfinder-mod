namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>Enemy HP and damage multipliers from the enemy scaling curve: a smooth path curve through Acts 2 and 3, and one value per act for bosses.</summary>
internal static class EnemyScalingCurve
{
    private const decimal Vanilla = 1m;

    private const decimal PathHpEndAct2 = 1.8m;
    private const decimal PathHpEndAct3 = 5.0m;
    private const decimal PathDamageEndAct2 = 1.5m;
    private const decimal PathDamageEndAct3 = 2.6m;

    private static readonly decimal[] BossHp = [Vanilla, 1.3m, 1.5m];
    private static readonly decimal[] BossDamage = [Vanilla, 1.15m, 1.25m];

    /// <param name="row">The point's row on the unified path, 1 for the first point.</param>
    /// <param name="pathLength">The number of points between the start and the boss.</param>
    public static decimal PathHp(int actIndex, int row, int pathLength) =>
        OnPath(actIndex, row, pathLength, PathHpEndAct2, PathHpEndAct3);

    public static decimal PathDamage(int actIndex, int row, int pathLength) =>
        OnPath(actIndex, row, pathLength, PathDamageEndAct2, PathDamageEndAct3);

    public static decimal BossHpFor(int actIndex) => BossHp[Math.Clamp(actIndex, 0, BossHp.Length - 1)];

    public static decimal BossDamageFor(int actIndex) => BossDamage[Math.Clamp(actIndex, 0, BossDamage.Length - 1)];

    private static decimal OnPath(int actIndex, int row, int pathLength, decimal endAct2, decimal endAct3)
    {
        if (actIndex <= 0)
            return Vanilla;

        var fraction = pathLength <= 1 ? 1m : Math.Clamp((row - 1m) / (pathLength - 1), 0m, 1m);
        return actIndex == 1
            ? Vanilla + (endAct2 - Vanilla) * fraction
            : endAct2 + (endAct3 - endAct2) * fraction;
    }
}
