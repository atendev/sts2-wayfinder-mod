using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>Doubles the weak encounter draws and the normal encounter list length, only inside <c>GenerateRooms</c>, for a run started with Wayfinder enabled.</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class ExtendEncounterListPatch
{
    private const int ExpectedCountReads = 3;

    private static readonly MethodInfo NumberOfWeakEncounters = AccessTools.PropertyGetter(typeof(ActModel), "NumberOfWeakEncounters");
    private static readonly MethodInfo GetNumberOfRooms = AccessTools.Method(typeof(ActModel), nameof(ActModel.GetNumberOfRooms));
    private static readonly MethodInfo Scale = AccessTools.Method(typeof(ExtendEncounterListPatch), nameof(ScaleCount));

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

        if (IsCountRead(instruction))
            yield return new CodeInstruction(OpCodes.Call, Scale);
    }

    private static int ScaleCount(int count) => WayfinderConfig.EnableWayfinder ? count * 2 : count;
}
