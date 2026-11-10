namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal static class CardRewardSchedule
{
    /// <param name="isNormalCombat">A Monster room, including a combat rolled by an Unknown point. Elites and bosses always give a card reward.</param>
    /// <param name="earlierNormalCombats">Normal combats already fought this run, not counting the current one.</param>
    public static bool GivesCardReward(bool alternate, bool isNormalCombat, int earlierNormalCombats) =>
        !alternate || !isNormalCombat || earlierNormalCombats % 2 == 0;
}
