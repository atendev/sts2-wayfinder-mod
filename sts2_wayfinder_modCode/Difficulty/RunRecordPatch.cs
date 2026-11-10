using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>Writes the run's difficulty record beside its history entry. Only Wayfinder runs get one, the Off preset included, so a run without one reads as Vanilla or Modded.</summary>
[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
internal static class RunRecordPatch
{
    private static void Postfix(SerializableRun run)
    {
        try
        {
            // A run abandoned from the main menu has no run state to read the snapshot from, so it gets no record.
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null || !UnifiedActMap.IsLine(runState.Map))
                return;

            var folder = ProjectSettings.GlobalizePath(
                System.IO.Path.Combine(RunHistorySaveManager.GetHistoryPath(SaveManager.Instance.CurrentProfileId), RunRecordSidecar.FolderName));
            System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, RunRecordSidecar.FileName(run.StartTime)),
                RunRecordSidecar.ToJson(DifficultySettings.For(runState)));
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"Could not write the run difficulty record: {e.Message}");
        }
    }
}
