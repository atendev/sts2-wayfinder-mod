using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>Swaps an Elite's relic reward for one with an explicit, lower rarity. It still always drops, and Black Star, events, and shops keep vanilla rarity.</summary>
[HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
internal static class EliteRelicRarityPatch
{
    private static void Postfix(Player player, AbstractRoom room, List<Reward> __result)
    {
        var runState = player.RunState;
        if (room.RoomType != RoomType.Elite || !UnifiedActMap.IsLine(runState.Map)
            || !DifficultySettings.For(runState).LowerEliteRelicRarity)
            return;

        var index = __result.FindIndex(reward => reward is RelicReward);
        if (index < 0)
            return;

        // Vanilla rolls rarity with one float from this stream in Populate. Rolling it here instead keeps the stream's
        // draw count the same, so a save and reload replays it.
        var rarity = EliteRelicRarity.Tier(player.PlayerRng.Rewards.NextFloat()) switch
        {
            EliteRelicTier.Common => RelicRarity.Common,
            EliteRelicTier.Uncommon => RelicRarity.Uncommon,
            _ => RelicRarity.Rare,
        };
        __result[index] = new RelicReward(rarity, player);
    }
}
