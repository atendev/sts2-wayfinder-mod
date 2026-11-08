using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
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

    private static readonly Func<NMapScreen, Vector2, Vector2, IReadOnlyList<TextureRect>> CreatePath =
        AccessTools.MethodDelegate<Func<NMapScreen, Vector2, Vector2, IReadOnlyList<TextureRect>>>(
            AccessTools.Method(typeof(NMapScreen), "CreatePath"));

    private static void Postfix(
        NMapScreen __instance,
        ActMap map,
        RunState ____runState,
        float ____distX,
        float ____distY,
        Dictionary<MapCoord, NMapPoint> ____mapPointDictionary,
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> ____paths,
        Vector2 ____tickTraveledScale)
    {
        if (!UnifiedActMap.IsLine(map))
            return;

        var points = map.GetAllMapPoints().OrderBy(point => point.coord.row).ToList();
        if (points.Count < SwitchbackLayout.MinPoints)
            return;

        var positions = SwitchbackLayout.Positions(points.Count, map.SecondBossMapPoint != null);
        for (var i = 0; i < points.Count; i++)
        {
            var coord = points[i].coord;
            var node = ____mapPointDictionary[coord];
            var stock = new Vector2(-500f + coord.col * ____distX, 740f - coord.row * ____distY);
            var jitter = node.Position - stock;
            node.Position = new Vector2(positions[i].X, positions[i].Y) + jitter;
        }

        foreach (var (link, dots) in ____paths.ToList())
        {
            foreach (var dot in dots)
                dot.QueueFreeSafely();

            ____paths[link] = CreatePath(
                __instance,
                GetLineEndpoint(__instance, ____mapPointDictionary[link.Item1]),
                GetLineEndpoint(__instance, ____mapPointDictionary[link.Item2]));
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
}
