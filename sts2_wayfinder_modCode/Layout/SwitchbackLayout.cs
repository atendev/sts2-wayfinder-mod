namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Layout;

/// <summary>Places a unified path's points on ranks that alternate direction, in map screen pixels.</summary>
internal static class SwitchbackLayout
{
    /// <summary>The fewest points that can fill middle ranks of 6 to 8.</summary>
    public const int MinPoints = 26;

    private const int EndRankPoints = 4;
    private const int MiddleRankPoints = 7;
    private const float LeftEdge = -500f;
    private const float RightEdge = 400f;
    private const float Center = -50f;
    private const float BottomY = 585f;

    /// <summary>Returns one position per point, in path order, from the bottom rank to the top rank.</summary>
    public static IReadOnlyList<(float X, float Y)> Positions(int pointCount, bool hasSecondBoss)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pointCount, MinPoints);

        var topY = 740f - 2325f * (hasSecondBoss ? 0.9f : 1f);
        var middlePoints = pointCount - 2 * EndRankPoints;
        var middleRanks = (int)Math.Round(middlePoints / (double)MiddleRankPoints);
        var topRank = middleRanks + 1;

        var positions = new List<(float X, float Y)>(pointCount);
        for (var rank = 0; rank <= topRank; rank++)
        {
            var size = rank == 0 || rank == topRank
                ? EndRankPoints
                : middlePoints / middleRanks + (rank <= middlePoints % middleRanks ? 1 : 0);
            var rightward = rank % 2 == 0;
            var from = rank == 0 ? Center : rightward ? LeftEdge : RightEdge;
            var to = rank == topRank ? Center : rightward ? RightEdge : LeftEdge;
            var y = BottomY + (topY - BottomY) * rank / topRank;

            for (var i = 0; i < size; i++)
                positions.Add((from + (to - from) * i / (size - 1), y));
        }

        return positions;
    }
}
