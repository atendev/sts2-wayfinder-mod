using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>
/// Resizes the weak encounter draws and the normal encounter list, only inside <c>GenerateRooms</c>, for a run started with Wayfinder enabled.
/// The weak draws double. The list doubles on the Full map path and is sized to the act's longest path on the Acts path.
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class ExtendEncounterListPatch
{
    private const int ExpectedCountReads = 3;

    private static readonly MethodInfo NumberOfWeakEncounters = AccessTools.PropertyGetter(typeof(ActModel), "NumberOfWeakEncounters");
    private static readonly MethodInfo GetNumberOfRooms = AccessTools.Method(typeof(ActModel), nameof(ActModel.GetNumberOfRooms));
    private static readonly MethodInfo ScaleWeak = AccessTools.Method(typeof(ExtendEncounterListPatch), nameof(ScaleWeakCount));
    private static readonly MethodInfo ScaleRooms = AccessTools.Method(typeof(ExtendEncounterListPatch), nameof(ScaleRoomCount));

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = instructions.ToList();
        var countReads = original.Count(IsCountRead);
        if (countReads != ExpectedCountReads)
        {
            MainFile.Logger.Error($"GenerateRooms reads an encounter count {countReads} times, expected {ExpectedCountReads}. Leaving the encounter list unchanged.");
            return original;
        }

        return original.SelectMany(WithScale);
    }

    private static bool IsCountRead(CodeInstruction instruction) =>
        instruction.Calls(NumberOfWeakEncounters) || instruction.Calls(GetNumberOfRooms);

    private static IEnumerable<CodeInstruction> WithScale(CodeInstruction instruction)
    {
        yield return instruction;

        if (instruction.Calls(NumberOfWeakEncounters))
        {
            yield return new CodeInstruction(OpCodes.Call, ScaleWeak);
        }
        else if (instruction.Calls(GetNumberOfRooms))
        {
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, ScaleRooms);
        }
    }

    private static int ScaleWeakCount(int count) => WayfinderConfig.EnableWayfinder ? count * 2 : count;

    private static int ScaleRoomCount(int count, ActModel act)
    {
        if (!WayfinderConfig.EnableWayfinder)
            return count;

        var state = RunManager.Instance.DebugOnlyGetState();
        var actIndex = state?.Acts.ToList().IndexOf(act) ?? -1;
        if (state == null || actIndex < 0 || DifficultySettings.PathLengthFor(state) != PathLength.Acts)
            return count * 2;

        return ActPlans.For(actIndex).MaxFightPoints;
    }
}
