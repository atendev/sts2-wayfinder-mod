# Map screen and travel (ticket 03)

Sources: IL and signatures in references/decompiled/sts2-il.txt and sts2.txt (game v0.107.1), the FightAllChoices dump, and the BaseLib 3.4.7 package under ~/.nuget. Everything is read from IL unless marked "inferred". Float constants in the field listing were decoded from little-endian hex by hand.

## 1. Grid coords to screen positions

`NMapScreen.SetMap(ActMap map, uint seed, bool clearDrawings)` does the layout. Rows and columns come from the grid: `GetRowCount()` is `Grid.GetLength(1)` and `GetColumnCount()` is `Grid.GetLength(0)`, so the grid is indexed `[col, row]`.

Spacing (instance fields set at the top of SetMap):
- `rows = map.GetRowCount()`, `cols = map.GetColumnCount()`
- `scale = 1.0`, or `0.9` when `map.SecondBossMapPoint != null`
- `_distY = 2325 / (rows - 1) * scale`
- `_distX = 1050 / cols`

Normal point position, in `_mapContainer`-local pixels, for every point from `GetAllMapPoints()`:
- `pos = (col, row) * (_distX, -_distY) + (-500, 740)`
- then a jitter of `x in [-21, 21]`, `y in [-25, 25]` from `new Rng(seed, "map_jitter_" + CurrentActIndex)`
- the node's icon rotation is `Rng.Chaotic.NextGaussianFloat(0, 8, 0, 1)` degrees (visual only)

Fixed-position nodes that ignore the grid:
- Starting point: `x = -80`, `y = row * -_distY + 800` (a normal start), or `+ 720` when the start type is Ancient (`MapPointType.Ancient` = 8). Its row comes from its own coord.
- Boss: `(-200, -1980 * scale)`. This is fixed, not derived from `rows`.
- Second boss, if present: `(-200, -2280 * scale)`, and both boss nodes get `Scale = (0.75, 0.75)`.

For vanilla (7 columns): `_distX = 150`, x runs from -500 (col 0) to 400 (col 6). `StandardActMap` builds `Grid = new MapPoint[7, mapLength]` with `mapLength = ActModel.GetNumberOfRooms(isMultiplayer) + 1`. `BaseNumberOfRooms` is 15 for act index 0, 14 for index 1, 13 for index 2, minus 1 in multiplayer. So grid rows are 16, 15, 14 (single player), giving `_distY` of 155, 166, 179 px. (BaseLib's xml doc says the same 15/14/13.)

Row meaning: the starting point is at `(cols/2, 0)`, the boss is at `(cols/2, GetRowCount())`, which is one past the last grid row, and the second boss is at `rows + 1`. Grid row 0 is empty in a standard map. Generated points live in rows 1 to rows-1. `ActMap.GetPoint(col,row)` checks boss, second boss and start before the grid, so those three are addressable even though they sit outside the array. `GetAllMapPoints()` enumerates the grid only, skipping nulls, so it never returns start or boss. `GetPointsInRow(r)` returns nothing for `r` outside `[0, rows)`.

Consequence for a unified path: each point needs a distinct `MapCoord`, and the dictionary `_mapPointDictionary` is keyed by it. A point's coord is also its identity in saves, votes and visited history.

## 2. Scrolling limits

All limits are hard-coded float literals inside the IL, not fields a mod can set. (The field listing shows `_scrollLimitTop` and `_scrollLimitBottom` as consts, 1800 and -600, but the methods use inline `ldc.r4` values, so changing them takes a transpiler or replacing the methods.)

- The scrolled thing is `_mapContainer`, driven by `_targetDragPos` (only Y matters). `UpdateScrollPosition` lerps the container toward the target at `delta * 15`, snapping when closer than 0.5 px.
- When not dragging, a target Y below -600 is pulled back toward `(0, -600)` and above 1800 toward `(0, 1800)`, both at `delta * 12`. Dragging, mouse wheel (`ScrollHelper.GetDragForScrollEvent`), and controller up/down (`_controllerScrollAmount = 400` per press) change `_targetDragPos` without clamping. The limit is a spring-back, not a wall.
- So the scrollable window is Y in [-600, 1800], a 2400 px travel. Vanilla content height is 2325 (the `2325` in `_distY`) plus 75 px margin.
- Initial and re-open position: `container.Y = -600 + currentRow * _distY`, where `currentRow` is the row of `RunState.CurrentMapCoord` (0 if none). `Open` sets this in both `_targetDragPos` and the container position. The controller "select" key does the same, and goes to `(0,-600)` when `ActFloor == 0`.
- The start-of-act animation (`StartOfActAnim`) sets the container to Y 1800, waits, then tweens to -600.
- The background is `NMapBg` with children named `MapTop`, `MapMid`, `MapBot` from the scene. Its code does not take the row count. Inferred: the art is a fixed-height tile stack matching the 2325 px layout, so a longer line needs either the art stretched or accepted as blank/repeating. The scene (.tscn) is not in the decompile, so this is unverified.

Implication: the row count only feeds `_distY`. With the stock formula a longer line gets tighter, not longer: N grid rows give `_distY = 2325 / (N - 1)`. For 60 points that is about 39 px between points. The node size comes from `normal_map_point.tscn`, which is not in the dump, so I cannot state when points overlap (inferred: icons are larger than 39 px, so they will). To keep a stock spacing you must patch the scroll constants (-600, 1800, and the 2325 and -1980 literals), or arrange points in several columns per row and link them sideways (what FightAllChoices does).

## 3. Drawing paths between points

`DrawPaths(NMapPoint node, MapPoint point)` loops `point.Children`. For each child it:
1. throws `InvalidOperationException` if the child coord is not in `_mapPointDictionary`;
2. computes `start = GetLineEndpoint(node)` and `end = GetLineEndpoint(childNode)`;
3. calls `CreatePath(start, end)` and does `_paths.Add((point.coord, child.coord), dots)`.

`_paths` is `Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>`. `Add` throws on a duplicate key, so a link must be added once per ordered pair (a `SetMap` re-run clears it first, so that is fine).

`GetLineEndpoint(NMapPoint)`: a `NNormalMapPoint` returns its `Position` (top-left); anything else (boss, ancient) returns `Position + Size * 0.5`.

`CreatePath(Vector2 start, Vector2 end)` makes a dotted line of `map_dot.tscn` sprites:
- `dir = (end - start).Normalized()`, `angle = dir.Angle() + PI/2`, `dist = start.DistanceTo(end)`
- `count = (int)(dist / 22) + 1`; dots at `start + dir * (i * 22)` for `i = 1 .. count-1`
- each dot is shifted by a fixed offset (`-(thisControl.Size * 0.5 - 20)`, as the IL reads it, using `ldarg.0` which is the screen), jittered by `Rng.Chaotic` in [-3, 3] on both axes, flipped randomly, rotated by `angle + gaussian(0, 0.1)`, modulated with `ActModel.MapUntraveledColor`, and added to `_pathsContainer`.
- Visited links are recolored in SetMap with `ActModel.MapTraveledColor` and scale `_tickTraveledScale = 1.2`.

It is a straight line between two screen points with no use of rows, columns or direction. So same-row (horizontal) links, links that go down, and links across many rows all draw fine. Nothing limits it to vanilla row counts. The only requirement is that every child is in `_mapPointDictionary` and every `Children` entry is drawn through `DrawPaths` during SetMap, or added by hand to `_paths` afterward (which is what FightAllChoices does with `EnsureDrawnSameRowPaths`).

The travel animation (`TravelToMapCoord`) looks up `_paths[(lastVisited, target)]` with `TryGetValue` and, if found, plays the dots in order (0.3 to 0.8 s total by fast-mode setting); if the key is missing it just skips the animation. So a missing path is harmless.

Focus (controller) neighbors in SetMap are set per row: left/right to neighbors in the same row by list order, up/down to self. With one point per row, controller navigation has no neighbor to move to. Inferred: this only affects controller focus, not mouse.

## 4. Can Grid be larger than vanilla, and what breaks

Yes. `ActMap.Grid` is a virtual `MapPoint[,]` and every consumer reads its dimensions at run time. Evidence:
- `SavedActMap` builds its grid from `SerializableActMap.GridWidth` and `GridHeight`, so saves round-trip any size. `SerializableActMap.FromActMap` serialises `GetAllMapPoints()` plus start, boss and second boss.
- The game ships `MockCraftedActMap(int width, int height, start, boss)` with `Put(col,row,type)`, so arbitrary sizes are already supported by a game class. (It has no `SecondBossMapPoint` override, so the base default is null. Whether `ActMap` declares that default I did not check.)
- `NMapScreen` uses `GetRowCount`/`GetColumnCount` only for the formulas in section 1 and for the boss rule below.

What does not adapt:
- `_distY`, scroll window and boss/start y-literals (sections 1 and 2). The map gets denser, not longer.
- `RecalculateTravelability` makes the boss `Travelable` when the last visited coord has `row == GetRowCount() - 1`. So the last gridded point must sit on the last grid row, and the boss coord should be `(col, GetRowCount())`.
- Grid column count sets `_distX = 1050 / cols`. A wider grid squeezes columns horizontally.
- `StandardActMap` hard-codes 7 columns and generates branches itself. You must supply your own `ActMap` (a subclass, or mutate the generated one in place: `MapPoint.AddChildPoint/RemoveChildPoint`, `Grid` array contents) because the standard class cannot be told a different size.
- Anything else keyed to `row` (inferred, not exhaustively traced): `NMapPoint.OnRelease` special-cases `row == 0` for the map-select tutorial; `RunState.ActFloor` and history are separate counters.
- Harmony hook point for changing a generated map: `Hook.AfterMapGenerated(IRunState, ActMap, int)` (static, returns `Task`) and `NMapScreen.SetMap` prefix/postfix. FightAllChoices uses both. A BaseLib act can also override `CustomActModel.CustomCreateMap(RunState, bool)` ("Override to generate a map using custom logic"), but that only applies to acts defined via BaseLib, not the vanilla acts. `ActModel.CreateMap` itself is non-virtual and just calls `StandardActMap.CreateFor`.

## 5. MapTravel.GetTravelablePointsFrom

Complete IL, small enough to state exactly:

```
GetTravelablePointsFrom(IRunState runState, MapPoint currentPoint):
    if Hook.ShouldAllowFreeTravel(runState):
        return runState.Map.GetPointsInRow(currentPoint.coord.row + 1)
    return currentPoint.Children
```

So legality is only "a child of the current point", or "any point in the next row" when a model's hook allows free travel. There is no check on column, no check on same-row, and no check that a child is on a higher row. A same-row or backward child is legal if it is in `Children`.

The only game caller of it is `NMapScreen.RecalculateTravelability`. That method:
- With nothing visited (`VisitedMapCoords` empty): only the starting node is `Travelable`.
- Otherwise marks every node `Untravelable`, then visited coords `Traveled`, then reads the last visited coord:
  - if there is a second boss and last == boss coord: second boss node becomes `Travelable`;
  - else if `last.row == GetRowCount() - 1`: the boss node becomes `Travelable` (regardless of `Children`);
  - else if the coord is not in the dictionary: logs an error and falls back to the starting node;
  - else each point from `GetTravelablePointsFrom(runState, node.Point)` becomes `Travelable`.
- `NMapPoint.IsTravelable` is `State == Travelable` and `IsTravelEnabled`, or the debug flag.

Note `Hook.ShouldAllowFreeTravel` with a current point on the last grid row returns `GetPointsInRow(rows)`, which is empty, since the boss is outside the grid.

Selection and votes: `OnMapPointSelectedLocally` queues a `VoteForMapCoordAction` with the coord and `mapGenerationCount`; I did not find any travel check against `Children` in this method, so legality is enforced by what the UI marks travelable. I did not trace `MapSelectionSynchronizer` / `RunManager` server-side validation. Treat multiplayer validation as unchecked.

For a strict line: if each point's only child is the next point, `Children` gives exactly one choice. Two Harmony options exist: build the `Children` chain correctly (the game then works untouched), or prefix `GetTravelablePointsFrom` as FightAllChoices does. Building `Children` is the lower-risk option since `DrawPaths`, saves (`WireChildren`) and `BFS_FindPath` all read `Children`/`parents`.

## 6. FightAllChoices private members in v0.107.1

All exist in the installed IL:
- `ActMap.startMapPoints`: field `HashSet<MapPoint> startMapPoints` on `ActMap`, set in the `ActMap` constructor, filled by `StandardActMap.GenerateMap` and restored by `SavedActMap`. It holds the row-1 entry points. Non-public.
- `NMapScreen._mapPointDictionary`: `Dictionary<MapCoord, NMapPoint>`. Exists. Note the dictionary also holds the boss, second boss and starting nodes.
- `NMapScreen._paths`: `Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>`. Exists.
- `NMapScreen.GetLineEndpoint(NMapPoint)` returns `Vector2`. Exists, private instance.
- `NMapScreen.CreatePath(Vector2, Vector2)` returns `IReadOnlyList<TextureRect>`. Exists, private instance.
- The patch targets also exist: `NMapScreen.SetMap(ActMap, uint, bool)`, `Hook.AfterMapGenerated(IRunState, ActMap, int)`, `MapTravel.GetTravelablePointsFrom(IRunState, MapPoint)`, `RunManager.DebugOnlyGetState`.

I could not check game v0.109.0, since only v0.107.1 is installed and decompiled. The wayfinder manifest (`sts2_wayfinder_mod.json`) currently says `min_game_version: 0.107.0` and BaseLib `min_version 3.4.7`; BaseLib 3.4.7 itself declares `min_game_version 0.107.1`. The "0.109.0" claim in the ticket is about the FightAllChoices mod, which the dump does not show. So for those members, 0.107.1 is verified and anything newer is unverified; use reflection with null checks and log on miss, as the reference mod does (its `.cctor` does `Type.GetField`/`GetMethod` by name).

## 7. BaseLib

`~/.nuget/packages/alchyr.sts2.baselib/3.4.7` is on disk (lib/net9.0/BaseLib.dll and BaseLib.xml). The only map-related items in its xml docs and strings are on `BaseLib.Abstracts.CustomActModel`: `BaseNumberOfRooms`, `GetMapPointTypes(Rng)`, `CustomCreateMap(RunState, bool)`, `CustomActMapTopBgPath/MidBgPath/BotBgPath`, plus custom `MapPointType` enum values. A strings scan of the DLL found no reference to `NMapScreen` or `MapTravel`. Nothing in BaseLib patches `NMapScreen`, layout, scrolling or travel, so none of it helps with a single long line. It helps only for acts BaseLib itself defines, and only as a hook for custom map generation and background art. I read documentation strings only; I did not disassemble the DLL.

## Summary of what a long single line needs

1. Provide an `ActMap` with a grid of at least `[cols, N + 1]` where the last gridded point is on row `rows - 1`, and a boss at `(col, rows)`. Or keep vanilla dimensions and use several points per row with same-row links.
2. Set `Children` as a single chain start -> p1 -> ... -> pN -> boss, so `GetTravelablePointsFrom` needs no patch.
3. If N is large, the stock `_distY = 2325 / (rows-1)` squeezes spacing. Fixing it needs a transpiler or reimplementation of `UpdateScrollPosition`, `Open`, `ProcessControllerEvent`, `StartOfActAnim` (the -600 and 1800 literals) and SetMap (2325, 740, 800 or 720, -1980), plus background art height.
4. Any link not created by `SetMap` (extra same-row links added after `AfterMapGenerated`) must be added to `_paths` through `CreatePath`, as FightAllChoices does, or it simply will not be drawn.
