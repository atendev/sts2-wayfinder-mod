using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>Marks a combat gold reward being paid, so <see cref="GoldRatePatch"/> doesn't scale it a second time.</summary>
internal static class GoldRewardPaying
{
    [ThreadStatic]
    public static bool Active;
}

/// <summary>Scales a combat gold reward when it is rolled, so the reward screen shows the gold the player will get.</summary>
[HarmonyPatch(typeof(GoldReward), nameof(GoldReward.Populate))]
internal static class GoldRewardPopulatePatch
{
    private static void Postfix(GoldReward __instance)
    {
        var runState = __instance.Player.RunState;
        if (!UnifiedActMap.IsLine(runState.Map))
            return;

        var scaled = GoldRateScaling.Apply(__instance.Amount, DifficultySettings.For(runState).GoldMultiplier);
        AccessTools.PropertySetter(typeof(GoldReward), nameof(GoldReward.Amount)).Invoke(__instance, [(int)scaled]);
    }
}

/// <summary>The gold gain starts synchronously inside <c>OnSelect</c>, before its first await, so the flag is set exactly while the hook runs.</summary>
[HarmonyPatch(typeof(GoldReward), "OnSelect")]
internal static class GoldRewardSelectPatch
{
    private static void Prefix() => GoldRewardPaying.Active = true;

    private static void Finalizer() => GoldRewardPaying.Active = false;
}
