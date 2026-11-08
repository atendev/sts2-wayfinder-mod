---
type: task
blocked_by: [07, 11]
undermined_by: []
assets: []
---

# Rewrite each act's map into a unified path

## Question

Add the Harmony prefix on `Hook.ModifyGeneratedMapLate` described in [the rewrite decision](./07-rewrite-integration.md). It skips `SavedActMap`, reads `EnableWayfinder` only for a new run's first map, seeds from `act_N_wayfinder`, keeps the start point at row 0, and writes one point per row in column 3 with an empty `startMapPoints`.

## Answer

Built in `sts2_wayfinder_modCode/UnifiedPath/`. `RewriteMapPatch` is the prefix and follows [the rewrite decision](./07-rewrite-integration.md). It replaces the hook's `map` argument, so the original method still runs the Late hooks on the new path. `UnifiedActMap` is the new `ActMap` subclass and `IsLine` is the Wayfinder line check.

The shape copies `GoldenPathActMap`: a 7-wide grid, the start point at (3,0) outside the grid, the path on rows 1 to N, and the boss at row N+1. Choices the ticket left open:

- A second boss stays, one row after the boss and as its child, matching `StandardActMap`.
- A `NullActMap` after act 1 counts as vanilla.
- A grid point of any type outside the six is counted as a Monster, the fallback vanilla uses for leftover points, so the path never loses a point.
- New points keep the constructor's `CanBeModified`, because only the pruning repair and the save code read it.
- Each rewrite logs one line with the point count and break step, for [the playthrough](./18-playthrough.md).

The v0.107.1 IL confirms that `Hook.ModifyGeneratedMapLate` returns its `map` argument, that both `GenerateMap` call sites pass `CurrentActIndex` as `actIndex`, and that `RunState.set_Map` runs after both hook calls.

Verified: the mod builds against `sts2.dll`, the 9 ordering tests pass, and a throwaway harness on the real `MapPoint`, `Rng`, and save classes confirmed every point lands on the path, the order is deterministic, a second boss survives, and the line check holds after a save round trip. Not verified: the Harmony patch firing in-game or a real `RunState`, which [the playthrough](./18-playthrough.md) covers.

Excluded:

- Setting `__result` and skipping the original, which would drop the Late hooks.
- Writing the rewrite inside `Ordering/`, which stays free of game references.
