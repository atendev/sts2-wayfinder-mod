using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal static class CardRewardState
{
    /// <summary>The Card choices option, set only while <c>GenerateRewardsFor</c> runs on a Wayfinder line, so relic, event, and Prayer Wheel card rewards keep their own counts.</summary>
    public static int? ChoiceCount { get; set; }

    /// <summary>Normal combats fought earlier in the current act, rebuilt from the map point history so a save and reload gives the same result. Unknown points that rolled a combat count; the current point never does.</summary>
    public static int EarlierNormalCombats(IRunState runState)
    {
        var act = runState.CurrentActIndex;
        if (act >= runState.MapPointHistory.Count)
            return 0;

        return runState.MapPointHistory[act]
            .Count(entry => !ReferenceEquals(entry, runState.CurrentMapPointHistoryEntry) && entry.HasRoomOfType(RoomType.Monster));
    }
}

/// <summary>Applies Card choices to the room's card reward and drops it on every second normal combat when card rewards alternate. Gold and potions stay.</summary>
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
internal static class CardRewardsPatch
{
    private static void Prefix(Player player)
    {
        CardRewardState.ChoiceCount = null;
        if (UnifiedActMap.IsLine(player.RunState.Map))
            CardRewardState.ChoiceCount = DifficultySettings.For(player.RunState).CardChoices;
    }

    private static void Postfix(Player player, AbstractRoom room, List<Reward> __result)
    {
        var runState = player.RunState;
        if (room.RoomType != RoomType.Monster || !UnifiedActMap.IsLine(runState.Map))
            return;

        if (CardRewardSchedule.GivesCardReward(DifficultySettings.For(runState).AlternateCardRewards, true,
                CardRewardState.EarlierNormalCombats(runState)))
            return;

        // Populate hasn't run, so a dropped card reward draws nothing from the reward stream. Its constructor already
        // subscribed to RelicObtained, and that handler throws before Populate, so unsubscribe it here.
        foreach (var reward in __result.OfType<CardReward>())
            player.RelicObtained -= (Action<RelicModel>)Delegate.CreateDelegate(typeof(Action<RelicModel>), reward, OnRelicObtained);
        __result.RemoveAll(reward => reward is CardReward);
    }

    private static readonly MethodInfo OnRelicObtained = AccessTools.Method(typeof(CardReward), "OnRelicObtained");

    private static void Finalizer() => CardRewardState.ChoiceCount = null;
}

[HarmonyPatch(typeof(CardReward), MethodType.Constructor,
    [typeof(CardCreationOptions), typeof(int), typeof(Player), typeof(PlayerChoiceSynchronizer), typeof(CardSelectorStack)])]
internal static class CardChoiceCountPatch
{
    private static void Prefix(ref int cardCount)
    {
        if (CardRewardState.ChoiceCount is { } count)
            cardCount = count;
    }
}
