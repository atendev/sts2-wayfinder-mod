using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

[HarmonyPatch(typeof(RelicModel), "IsBeforeAct3TreasureChest")]
internal static class Act3RelicCheckPatch
{
    private const int Act3Index = 2;

    private static void Postfix(IRunState runState, ref bool __result)
    {
        if (UnifiedActMap.IsLine(runState.Map))
            __result = runState.CurrentActIndex < Act3Index;
    }
}
