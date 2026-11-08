using MegaCrit.Sts2.Core.Map;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Ordering;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.UnifiedPath;

/// <summary>A strict line from an act's start point to its boss, one point per row in the center column.</summary>
internal sealed class UnifiedActMap : ActMap
{
    private const int Width = 7;
    private const int Column = 3;

    public override MapPoint StartingMapPoint { get; }
    public override MapPoint BossMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }
    protected override MapPoint[,] Grid { get; }

    private UnifiedActMap(ActMap source, IReadOnlyList<PointType> path)
    {
        Grid = new MapPoint[Width, path.Count + 1];
        StartingMapPoint = new MapPoint(Column, 0) { PointType = source.StartingMapPoint.PointType };

        var previous = StartingMapPoint;
        for (var i = 0; i < path.Count; i++)
        {
            var row = i + 1;
            var point = new MapPoint(Column, row) { PointType = ToGameType(path[i]) };
            Grid[Column, row] = point;
            previous.AddChildPoint(point);
            previous = point;
        }

        BossMapPoint = new MapPoint(Column, path.Count + 1) { PointType = source.BossMapPoint.PointType };
        previous.AddChildPoint(BossMapPoint);

        if (source.SecondBossMapPoint is not { } secondBoss)
            return;

        SecondBossMapPoint = new MapPoint(Column, path.Count + 2) { PointType = secondBoss.PointType };
        BossMapPoint.AddChildPoint(SecondBossMapPoint);
    }

    public static UnifiedActMap From(ActMap source, string seed, int actNumber)
    {
        var result = PathOrderer.Order(MixOf(source), new SeededOrderingRng(seed, $"act_{actNumber}_wayfinder"));
        MainFile.Logger.Info($"Act {actNumber}: unified path of {result.Path.Count} points, break step {result.Step}");
        return new UnifiedActMap(source, result.Path);
    }

    /// <summary>True for a map with one point per row, all in the center column, and no start points.</summary>
    public static bool IsLine(ActMap map)
    {
        if (map.startMapPoints.Count != 0)
            return false;

        var rows = new HashSet<int>();
        foreach (var point in map.GetAllMapPoints())
        {
            if (point.coord.col != Column || !rows.Add(point.coord.row))
                return false;
        }

        return rows.Count > 0;
    }

    private static PointMix MixOf(ActMap source)
    {
        var counts = source.GetAllMapPoints().CountBy(point => point.PointType).ToDictionary();
        int Count(MapPointType type) => counts.GetValueOrDefault(type);

        var mix = new PointMix(
            Elites: Count(MapPointType.Elite),
            Unknowns: Count(MapPointType.Unknown),
            Shops: Count(MapPointType.Shop),
            RestSites: Count(MapPointType.RestSite),
            Treasures: Count(MapPointType.Treasure));

        return mix with { Monsters = counts.Values.Sum() - mix.Total };
    }

    private static MapPointType ToGameType(PointType type) => type switch
    {
        PointType.Monster => MapPointType.Monster,
        PointType.Elite => MapPointType.Elite,
        PointType.Unknown => MapPointType.Unknown,
        PointType.Shop => MapPointType.Shop,
        PointType.RestSite => MapPointType.RestSite,
        PointType.Treasure => MapPointType.Treasure,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
