using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Layout;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>Moves a unified path's points into a switchback after the stock screen has built them, and redraws the links.</summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
internal static class DrawSwitchbackPatch
{
    private static readonly Func<NMapScreen, NMapPoint, Vector2> GetLineEndpoint =
        AccessTools.MethodDelegate<Func<NMapScreen, NMapPoint, Vector2>>(
            AccessTools.Method(typeof(NMapScreen), "GetLineEndpoint"));

    private static void Postfix(
        NMapScreen __instance,
        ActMap map,
        ulong seed,
        RunState ____runState,
        Control ____points,
        Control ____pathsContainer,
        Dictionary<MapCoord, NMapPoint> ____mapPointDictionary,
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> ____paths,
        Vector2 ____tickTraveledScale)
    {
        if (!UnifiedActMap.IsLine(map))
            return;

        var points = map.GetAllMapPoints().OrderBy(point => point.coord.row).ToList();
        if (points.Count < SwitchbackLayout.MinPoints)
            return;

        // SwitchbackLayout is authored in the point container's center frame. The stock screen now shifts points by half of
        // the container size, so apply the same shift, and replay its jitter stream so the points keep their stock wobble.
        var jitter = Jitter(map, seed, ____runState.CurrentActIndex);
        var origin = ____points.Size * 0.5f;
        var positions = SwitchbackLayout.Positions(points.Count, map.SecondBossMapPoint != null);
        for (var i = 0; i < points.Count; i++)
        {
            var coord = points[i].coord;
            ____mapPointDictionary[coord].Position = new Vector2(positions[i].X, positions[i].Y) + origin + jitter[coord];
        }

        foreach (var (link, dots) in ____paths.ToList())
        {
            foreach (var dot in dots)
                dot.QueueFreeSafely();

            ____paths[link] = NMapScreen.CreatePath(
                GetLineEndpoint(__instance, ____mapPointDictionary[link.Item1]),
                GetLineEndpoint(__instance, ____mapPointDictionary[link.Item2]),
                ____pathsContainer,
                ____runState.Act.MapUntraveledColor);
        }

        var visited = ____runState.VisitedMapCoords;
        for (var i = 0; i < visited.Count - 1; i++)
        {
            if (!____paths.TryGetValue((visited[i], visited[i + 1]), out var dots))
                continue;

            foreach (var dot in dots)
            {
                dot.Modulate = ____runState.Act.MapTraveledColor;
                dot.Scale = ____tickTraveledScale;
            }
        }
    }

    private static Dictionary<MapCoord, Vector2> Jitter(ActMap map, ulong seed, int actIndex)
    {
        var rng = new Rng(seed, $"map_jitter_{actIndex}");
        var jitter = new Dictionary<MapCoord, Vector2>();
        foreach (var point in map.GetAllMapPoints())
        {
            var x = rng.NextFloat(-21f, 21f);
            var y = rng.NextFloat(-25f, 25f);
            jitter[point.coord] = new Vector2(x, y);
        }
        return jitter;
    }
}
