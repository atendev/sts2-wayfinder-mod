using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal static class TreasureChestState
{
    /// <summary>True only while <c>BeginRelicPicking</c> runs for a gold chest, so the game's own empty-chest path skips the relic.</summary>
    public static bool SuppressRelic { get; set; }

    private static (int Act, MapCoord Coord, decimal GoldMultiplier)? _goldChest;

    public static void MarkGoldChest(IRunState runState, MapCoord coord, decimal goldMultiplier) =>
        _goldChest = (runState.CurrentActIndex, coord, goldMultiplier);

    public static void Clear() => _goldChest = null;

    /// <summary>The gold multiplier if the chest in this room was rolled as a gold chest.</summary>
    public static decimal? GoldMultiplierFor(IRunState runState) =>
        _goldChest is { } chest && chest.Act == runState.CurrentActIndex && runState.CurrentMapCoord == chest.Coord
            ? chest.GoldMultiplier
            : null;

    /// <summary>The act's earlier chests rebuilt from the map point history, so a save and reload gives the same chance. A chest that only gave gold counts as a gold chest, even when the player skipped its relic.</summary>
    public static IEnumerable<ChestResult> EarlierChests(IRunState runState)
    {
        var act = runState.CurrentActIndex;
        if (act >= runState.MapPointHistory.Count)
            yield break;

        foreach (var entry in runState.MapPointHistory[act])
        {
            if (!entry.HasRoomOfType(RoomType.Treasure) || ReferenceEquals(entry, runState.CurrentMapPointHistoryEntry))
                continue;

            yield return entry.PlayerStats.Any(stats => stats.RelicChoices.Count > 0) ? ChestResult.Relic
                : entry.PlayerStats.Any(stats => stats.GoldGained > 0) ? ChestResult.Gold
                : ChestResult.Empty;
        }
    }

    public static bool IsLastChestOfAct(IRunState runState, MapCoord coord) =>
        runState.Map.GetAllMapPoints().Where(point => point.PointType == MapPointType.Treasure)
            .All(point => point.coord.row <= coord.row);
}

/// <summary>Rolls the chest outcome on the run's <c>TreasureRoomRelics</c> stream when the room opens, only for a chest that would give a treasure.</summary>
[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
internal static class TreasureChestOutcomePatch
{
    private static void Prefix(IPlayerCollection ____playerCollection, Rng ____rng)
    {
        TreasureChestState.Clear();

        var runState = ____playerCollection.Players.FirstOrDefault()?.RunState;
        if (runState == null || !UnifiedActMap.IsLine(runState.Map) || runState.CurrentMapCoord is not { } coord)
            return;

        var odds = DifficultySettings.For(runState).Treasure;
        if (!odds.Enabled)
            return;

        // Silver Crucible's emptied chest gets no roll, and its owner is the only player it skips.
        if (!____playerCollection.Players.Any(player => Hook.ShouldGenerateTreasure(runState, player)))
            return;

        var isRelicChest = TreasureChestTable.IsRelicChest(odds, TreasureChestState.EarlierChests(runState),
            TreasureChestState.IsLastChestOfAct(runState, coord), ____rng.NextFloat());
        if (isRelicChest)
            return;

        TreasureChestState.SuppressRelic = true;
        TreasureChestState.MarkGoldChest(runState, coord, odds.GoldMultiplier);
    }

    private static void Finalizer() => TreasureChestState.SuppressRelic = false;
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldGenerateTreasure))]
internal static class SuppressChestRelicPatch
{
    private static void Postfix(ref bool __result)
    {
        if (TreasureChestState.SuppressRelic)
            __result = false;
    }
}

/// <summary>Pays a gold chest's multiplied gold. It still draws vanilla's <c>NextInt(42, 53)</c> so the player's reward stream stays aligned.</summary>
[HarmonyPatch(typeof(OneOffSynchronizer), "DoTreasureRoomRewards")]
internal static class GoldChestRewardPatch
{
    private static readonly MethodInfo TryHandleSpoilsMap = AccessTools.Method(typeof(OneOffSynchronizer), "TryHandleSpoilsMap");

    private static bool Prefix(OneOffSynchronizer __instance, Player player, ref Task<int> __result)
    {
        if (TreasureChestState.GoldMultiplierFor(player.RunState) is not { } multiplier)
            return true;

        __result = Pay(__instance, player, multiplier);
        return false;
    }

    private static async Task<int> Pay(OneOffSynchronizer synchronizer, Player player, decimal multiplier)
    {
        if (!Hook.ShouldGenerateTreasure(player.RunState, player))
            return 0;

        var gold = player.PlayerRng.Rewards.NextInt(42, 53) * (double)multiplier;
        if (AscensionHelper.HasAscension(AscensionLevel.Poverty))
            gold *= AscensionHelper.PovertyAscensionGoldMultiplier;

        await PlayerCmd.GainGold((int)gold, player);
        var spoils = await (Task<int>)TryHandleSpoilsMap.Invoke(synchronizer, [player])!;
        return (int)gold + spoils;
    }
}
