---
type: research
blocked_by: []
undermined_by: []
assets: [02-map-lifecycle-and-dependents.md]
---

# Trace the map lifecycle and what depends on map shape

## Question

When is an act map created, and when does `Hook.AfterMapGenerated` fire? Does it fire again when a run is loaded from a save, given `SavedActMap`? How do `VisitedMapCoords` and the current map coord restore on load? What other game code reads or replaces the map, or depends on row numbers or branching? Check `SpoilsActMap`, `GoldenPathActMap`, map-altering events, cards and relics, and anything using floor or row indexes.

## Answer

Findings are in [the lifecycle research](../assets/02-map-lifecycle-and-dependents.md).

`RunManager.GenerateMap` is the only map builder. `Hook.AfterMapGenerated` fires on both a fresh map and a saved load, so a rewrite there must not run twice. Only the current act's map is saved. The current coord is the last entry of `VisitedMapCoords`, and coords missing from the new map get dropped.

`GoldenCompass`, `SpoilsMap`, and `BigGameHunter` replace the whole map in early hooks, so the conversion has to run after them. `GoldenPathActMap` is a single-column strict line already in the game.

`RunState.GetHistoryEntryFor` indexes history by `coord.row`, confirmed in IL. Its callers are the point hover in `NMapPoint.OnFocus` and the multiplayer reward handler, so several points sharing a row would show the wrong history. `RunState.TotalFloor` counts visited points and feeds `EncounterModel.GenerateMonstersWithSlots`, the `PunchOff` and `SlipperyBridge` event conditions, and `RelicModel.IsBeforeAct3TreasureChest`.

Act 1 starts on a Monster point instead of an Ancient when the run did not start with Neow.
