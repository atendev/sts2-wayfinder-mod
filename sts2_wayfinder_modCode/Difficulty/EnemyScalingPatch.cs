using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal static class EnemyScalingPatch
{
    /// <summary>The multiplier for a fight, or 1 when scaling is off for it or the run isn't a Wayfinder line.</summary>
    internal static decimal Factor(IRunState runState, RoomType? roomType, bool hp)
    {
        if (!UnifiedActMap.IsLine(runState.Map))
            return 1m;

        var toggles = DifficultySettings.For(runState).EnemyScaling;

        var act = runState.CurrentActIndex;
        if (roomType == RoomType.Boss)
        {
            if (!toggles.ScaleBosses)
                return 1m;
            return hp ? EnemyScalingCurve.BossHpFor(act) : EnemyScalingCurve.BossDamageFor(act);
        }

        if (hp ? !toggles.ScalePathHp : !toggles.ScalePathDamage)
            return 1m;
        if (runState.CurrentMapCoord is not { } coord)
            return 1m;

        var pathLength = runState.Map.BossMapPoint.coord.row - 1;
        return hp
            ? EnemyScalingCurve.PathHp(act, coord.row, pathLength)
            : EnemyScalingCurve.PathDamage(act, coord.row, pathLength);
    }
}

[HarmonyPatch(typeof(CombatState), nameof(CombatState.CreateCreature))]
internal static class EnemyHpScalingPatch
{
    private static void Postfix(CombatState __instance, MonsterModel monster, CombatSide side, Creature __result)
    {
        if (side != CombatSide.Enemy || __instance.Encounter is not { } encounter)
            return;

        // Summons and splits spawn after the encounter's own monsters and keep their vanilla HP, so a split never scales an already scaled parent twice.
        if (encounter.MonstersWithSlots.All(entry => entry.Item1 != monster))
            return;

        var factor = EnemyScalingPatch.Factor(__instance.RunState, encounter.RoomType, hp: true);
        if (factor == 1m)
            return;

        __result.SetMaxHpInternal(Math.Round(__result.MaxHp * factor, MidpointRounding.AwayFromZero));
        __result.SetCurrentHpInternal(__result.MaxHp);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class EnemyDamageScalingPatch
{
    private static void Postfix(IRunState runState, ICombatState? combatState, Creature? dealer, ValueProp props,
        ModifyDamageHookType modifyDamageHookType, ref decimal __result)
    {
        if (dealer is not { Side: CombatSide.Enemy, Monster: not null } || !props.IsPoweredAttack()
            || !modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative))
            return;

        var factor = EnemyScalingPatch.Factor(runState, combatState?.Encounter?.RoomType, hp: false);
        if (factor != 1m)
            __result = Math.Round(__result * factor, MidpointRounding.AwayFromZero);
    }
}
