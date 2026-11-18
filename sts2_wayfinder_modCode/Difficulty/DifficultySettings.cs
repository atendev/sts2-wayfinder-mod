using System.Runtime.CompilerServices;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>The run's difficulty snapshot. Every consumer reads it here and never the config, so a run keeps the settings it started with, including after a save and reload.</summary>
internal static class DifficultySettings
{
    private const string SaveId = "wayfinder_difficulty";

    private sealed class Snapshot(DifficultyOptions options)
    {
        public DifficultyOptions Options { get; } = options;
    }

    private static readonly ConditionalWeakTable<IRunState, Snapshot> Snapshots = new();

    /// <summary>The settings the run started with, or Off when it has none, as a run started before v0.2.0 or without Wayfinder.</summary>
    public static DifficultyOptions For(IRunState runState) =>
        Snapshots.TryGetValue(runState, out var snapshot) ? snapshot.Options : DifficultyOptions.Off;

    /// <summary>The path length the run started with. A run with no snapshot keeps the full map it was generated with.</summary>
    public static PathLength PathLengthFor(IRunState runState) =>
        Snapshots.TryGetValue(runState, out var snapshot) ? snapshot.Options.PathLength : PathLength.FullMap;

    public static void Take(IRunState runState, DifficultyOptions options)
    {
        Snapshots.Remove(runState);
        Snapshots.Add(runState, new Snapshot(options));
    }

    /// <summary>Saves the snapshot in <c>current_run.save</c> through BaseLib's extended run save data. Call from the mod initializer.</summary>
    public static void RegisterSave()
    {
        var registered = ExtendedSaveTypes.RegisterSavedValue<IRunState, string>(SaveId,
            runState => Snapshots.TryGetValue(runState, out var snapshot) ? snapshot.Options.ToJson() : null!,
            (runState, json) =>
            {
                if (!string.IsNullOrWhiteSpace(json))
                    Take(runState, DifficultyOptions.FromJson(json));
            },
            (json, writer) => writer.WriteString(json),
            reader => reader.ReadString());

        if (!registered)
            MainFile.Logger.Warn("Could not register the difficulty snapshot for the run save; runs will read as Off after a reload.");
    }
}

/// <summary>Snapshots the config into a new Wayfinder run before its first map is generated.</summary>
[HarmonyPatch]
internal static class SnapshotDifficultyPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewSingleplayer));
        yield return AccessTools.Method(typeof(RunManager), nameof(RunManager.SetUpNewMultiplayer));
    }

    private static void Prefix(RunState state)
    {
        if (WayfinderConfig.EnableWayfinder)
            DifficultySettings.Take(state, WayfinderConfig.CurrentOptions());
    }
}
