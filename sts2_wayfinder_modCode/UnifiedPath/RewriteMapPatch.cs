using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>Swaps a fresh act map for its unified path, after the Early map hooks and before the Late ones.</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGeneratedMapLate))]
internal static class RewriteMapPatch
{
    private static void Prefix(IRunState runState, ref ActMap map)
    {
        if (map is SavedActMap || !IsWayfinderRun(runState))
            return;

        if (map is GoldenPathActMap)
        {
            map.startMapPoints.Clear();
            return;
        }

        map = UnifiedActMap.From(map, runState.Rng.Seed, runState.CurrentActIndex + 1);
    }

    private static bool IsWayfinderRun(IRunState runState)
    {
        if (runState.Map is NullActMap)
            return runState.CurrentActIndex == 0 && WayfinderConfig.EnableWayfinder;

        return UnifiedActMap.IsLine(runState.Map);
    }
}
