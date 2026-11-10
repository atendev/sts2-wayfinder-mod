using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>Applies the Gold rate to every gold gain, since combat, chest, event, and relic gold all pay through <c>PlayerCmd.GainGold</c>. The chest multiplier is applied before that call, so the two stack. Shop prices use <c>LoseGold</c> and are untouched.</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGoldGained))]
internal static class GoldRatePatch
{
    private static void Postfix(IRunState runState, ref decimal __result)
    {
        if (!UnifiedActMap.IsLine(runState.Map))
            return;

        __result = GoldRateScaling.Apply(__result, DifficultySettings.For(runState).GoldMultiplier);
    }
}
