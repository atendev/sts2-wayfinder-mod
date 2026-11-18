using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>Rewrites the Elite and normal encounter lists after <c>GenerateRooms</c> for a run started with Wayfinder enabled, so picks stay random after the vanilla list runs out.</summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
internal static class RandomEncounterPicksPatch
{
    private static readonly AccessTools.FieldRef<ActModel, RoomSet> Rooms = AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");
    private static readonly AccessTools.FieldRef<RoomSet, List<EncounterModel>> NormalEncounters = AccessTools.FieldRefAccess<RoomSet, List<EncounterModel>>("normalEncounters");
    private static readonly AccessTools.FieldRef<RoomSet, List<EncounterModel>> EliteEncounters = AccessTools.FieldRefAccess<RoomSet, List<EncounterModel>>("eliteEncounters");

    private static void Postfix(ActModel __instance, Rng rng)
    {
        if (!WayfinderConfig.EnableWayfinder)
            return;

        // The lists ride in the run save and a loaded run never calls GenerateRooms again, so rewriting them here is what
        // keeps the picks stable across a save and reload. The draws come from the generation rng the vanilla lists used.
        var rooms = Rooms(__instance);
        var source = new GameRng(rng);
        EliteEncounters(rooms) = EncounterPicks.BuildEliteList(EliteEncounters(rooms), __instance.AllEliteEncounters.ToList(), EncounterPicks.EliteListLength, source);
        NormalEncounters(rooms) = EncounterPicks.ExtendNormalList(NormalEncounters(rooms), __instance.AllRegularEncounters.ToList(), EncounterPicks.NormalListLength, source);
    }

    private sealed class GameRng(Rng rng) : IOrderingRng
    {
        public int NextInt(int maxExclusive) => rng.NextInt(maxExclusive);
    }
}
