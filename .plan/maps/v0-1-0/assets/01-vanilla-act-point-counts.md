# Vanilla act point counts (game v0.107.1)

Source: references/decompiled/sts2-il.txt (IL) and sts2.txt (call lists). Everything below is read from IL unless marked **inferred**. "Point" means any stop on the map. Rows are `MapCoord.row`.

## Grid and path generation

- Grid is 7 columns wide (`_mapWidth` = 7) by `mapLength` rows, where `mapLength = GetNumberOfRooms(isMultiplayer) + 1`. `isMultiplayer` is `Players.Count > 1` (StandardActMap.CreateFor).
- Row 0 holds only the Ancient start point (`StartingMapPoint`, col = width/2, not stored in the grid array). Generated points live in rows 1 through `rooms`. The boss is a separate point at row `mapLength` (outside the grid array). A second boss, when present, is at row `mapLength + 1` and is a child of the first boss.
- So an act has `rooms` generated rows, plus the Ancient and the boss (plus an optional second boss). `ActModel.GetNumberOfFloors` returns `rooms + 2`.
- `GenerateMap` draws `_iterations` = 7 paths. Each path starts at a random column in row 1 (`NextInt(0,7)`); only the second path is forced to start in a column no earlier path used. Paths 3 to 7 may reuse a start column. Each step goes to row+1 and column c-1, c, or c+1 (clamped to 0..6), tried in a shuffled order; the first option that does not cross an existing edge wins (`HasInvalidCrossover`). Paths merge freely when they land on an existing point.
- Every row-1 point becomes a child of the Ancient. Every last-row point (row `rooms`) becomes a parent of the boss.
- After typing, `MapPathPruning.PruneAndRepair` runs (3 passes, only when the constructor flag is true, which CreateFor always sets). It removes duplicated path segments of 3 or more points that are shared between routes, then `RepairPrunedPointTypes` tops Shop, Elite, Rest, and Unknown back up to their target counts by converting unlocked Monster points that pass `IsValidPointType`. Then `CenterGrid`, `SpreadAdjacentMapPoints`, `StraightenPaths` run. **Inferred:** the three post-processing steps only move columns and do not change point counts; the prune step is the only thing that deletes points.
- Total point count is therefore random and not a closed formula. **Inferred (my own simulation, not game code):** I modelled the 7-path walk with the crossover rule in Python and no pruning. Before pruning, a 15-room act gave a mean of about 66 points (5th to 95th percentile 57 to 73, min 37, max 82), 14 rooms about 62, 13 rooms about 57. Row width averages about 4.7 points in row 1 and about 4.4 in later rows. Pruning lowers these, by an unmeasured amount. Per row the hard bounds are 1 to 7 points (row 1: 2 to 7). The exact total needs in-game measurement.

## Rooms, rests, unknowns, shops, elites per act

`ActModel.GetMapPointTypes(rng)` builds a `MapPointTypeCounts(unknownCount, restCount)`. The constructor sets shops and elites. `Rng.NextGaussianInt(mean, sd, min, max)` is rejection sampling: it redraws until the rounded Gaussian lands in [min, max] inclusive. `StandardRandomUnknownCount` = `NextGaussianInt(12, 1, 10, 14)`.

| Act (index) | Class | Rooms | mapLength | Rests (random, excludes fixed last row) | Unknowns | Shops | Elites |
|---|---|---|---|---|---|---|---|
| 1 (index 0) | Overgrowth (default) | 15 | 16 | `NextGaussianInt(7,1,6,7)` = 6 or 7 | Standard = 10 to 14 | 3 | 5, or 8 at A1+ |
| 1 alt (index 0) | Underdocks (IsDefault false) | 15 | 16 | `NextGaussianInt(7,1,6,7)` = 6 or 7 | Standard = 10 to 14 | 3 | 5 / 8 |
| 2 (index 1) | Hive | 14 | 15 | `NextGaussianInt(6,1,6,7)` = 6 or 7 | Standard - 1 = 9 to 13 | 3 | 5 / 8 |
| 3 (index 2) | Glory | 13 | 14 | `NextInt(5,7)` = 5 or 6 (uniform) | Standard - 1 = 9 to 13 | 3 | 5 / 8 |

- Multiplayer (more than one player) subtracts 1 from rooms: 14 / 13 / 12.
- **Inferred:** the Gaussian rest draws favor the nearer bound. For mean 7 on [6,7] about 61% give 7 and 39% give 6; for mean 6 on [6,7] about 61% give 6 and 39% give 7. This is plain normal-distribution arithmetic, not read from IL.
- `DeprecatedAct.GetMapPointTypes` returns (0, 0); ignore it.
- Elites: `Round(5 * (HasAscension(SwarmingElites) ? 1.6 : 1))` = 5, or 8 at ascension 1 and above. Shops: always 3 (`NumOfShops`).
- A rest is also placed on every last-row point (see below), so total rests are higher than the table's random count.

## What AssignPointTypes fixes

Fixed rows get a type directly with `CanBeModified = false` and skip all `IsValid*` rules. In order:

1. Last generated row (row `rooms`, the row before the boss): every point is a RestSite.
2. Treasure row at row `mapLength - 7` = `rooms - 6`: every point is Treasure (row 9 in Overgrowth/Underdocks, 8 in Hive, 7 in Glory, in 1-indexed generated rows). If `ShouldReplaceTreasureWithElites` is true, that row is all Elite instead.
3. Row 1 (first row): every point is Monster.

Then a queue is built in this order: `NumOfRests` x RestSite, `NumOfShops` x Shop, `NumOfElites` x Elite, `NumOfUnknowns` x Unknown. `AssignRemainingTypesToRandomPoints` runs up to 3 passes. Each pass shuffles the still-unassigned points and walks them; for each point it takes the next queue type that passes `IsValidPointType` (a type that fails is re-queued) and stops when the queue empties. Types a point cannot take are left for later points. Any queue entries still unplaced after 3 passes are dropped (the pruning repair step may re-add some). Every point still unassigned becomes Monster. Boss and Ancient are set last.

So the random counts above are targets, not guarantees. Fixed-row contributions add on top and depend on row widths (about 4 to 5 points each, 1 to 7 possible).

## IsValidPointType rules (random assignment only)

A candidate type must pass all five checks:

- Lower: if row < 6, no RestSite and no Elite. (Rows 1 to 5 are Monster, Shop, Unknown, or Treasure-row only.)
- Upper: if row >= `mapLength - 3` (= `rooms - 2`), no RestSite.
- Parents: for Elite, RestSite, Treasure, Shop, no parent or child may already have the same type.
- Children: same set; no child may have the same type.
- Siblings (other children of this point's parents): no sibling may share the type for RestSite, Monster, Unknown, Elite, Shop. Note Unknown is restricted here, so two Unknowns cannot be siblings.
- A type in `PointTypesThatIgnoreRules` skips all of this (set only by the BigGameHunter modifier, for Elite).
- Treasure is not in the random queue, so the static restriction sets mention it but it never reaches random assignment (**inferred** from the queue contents).

## Second boss, treasure replacement, ascension

- Second boss: `HasSecondBoss` comes from the act's `RoomSet`. `RunManager.GenerateRooms` calls `SetSecondBossEncounter` only for the last act (act index = acts count - 1) and only when `AscensionManager.HasLevel(10)` (DoubleBoss). Result: one extra Boss point, so Glory on ascension 10 has two bosses in a row (the second boss is a child of the first). Adds 1 point.
- `ShouldReplaceTreasureWithElites`: the only vanilla caller, `RunManager.GenerateMap`, calls `ActModel.CreateMap(runState, false)`. The debug map also passes false. The flag stays false in the base game. `BigGameHunter.ModifyGeneratedMap` re-creates the map and forwards the existing flag, and rebuilds counts with `NumOfElites = Round(MonsterPointCount * 2.5)` and Elite in `PointTypesThatIgnoreRules` (the exact point filter in `<ModifyGeneratedMap>b__0_0` was not opened; **inferred** to be Monster points). If a mod ever passes true, the treasure row becomes all Elite and the treasure point type disappears.
- Ascension effects on the map: A1 (SwarmingElites) raises the elite target from 5 to 8; A10 (DoubleBoss) adds the second boss in the last act. No other ascension level touches map generation (the other `HasAscension` calls found are gold rewards at level 3).
- Other map classes exist and bypass all of the above: `SpoilsActMap` (Spoils map card) and `GoldenPathActMap` (Golden Compass relic, one chosen act). Not read in detail.

## Key constants quick reference

Width 7; paths 7; rooms 15 / 15 / 14 / 13 (Overgrowth, Underdocks, Hive, Glory), minus 1 in multiplayer; shops 3; elites 5 or 8; first row Monster; treasure row `rooms - 6`; last row RestSite; no random Rest/Elite before row 6; no random Rest in the final 3 rows.
