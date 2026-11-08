---
type: grilling
blocked_by: [02]
undermined_by: []
assets: []
---

# Decide where the rewrite happens and how saves behave

## Question

At what point in the map lifecycle does the mod rewrite the map, and how does it stay correct across save and load without rewriting twice? What does the `EnableWayfinder` toggle do when flipped mid-run or between acts? What should happen to a run started before the mod was installed, or with the toggle off? Which RNG drives the shuffle so the same seed gives the same path?

Research constraints: `Hook.AfterMapGenerated` also fires on load, and the conversion must run after the early hooks where `GoldenCompass`, `SpoilsMap`, and `BigGameHunter` replace the map. Act 1 starts on a Monster point when the run did not start with Neow, which conflicts with the rule that each act starts at its Ancient. Decide whether the path keeps that vanilla start.

## Answer

The rewrite is a Harmony prefix on `Hook.ModifyGeneratedMapLate`. On a fresh map it runs after the Early hooks where `GoldenCompass`, `SpoilsMap`, and `BigGameHunter` replace the map, and before the Late hooks. `SpoilsMap` acts in both phases: its Early hook can swap in `SpoilsActMap`, and its Late hook picks a Treasure point. That Late hook and `FurCoat`'s pick from the finished path. The prefix can return a new map object. The path uses map rows 0 to N, one point per row, as [the layout decision](./05-path-layout-on-screen.md) requires, so history lookups by row stay correct. The grid grows to fit, and any grid size saves and draws.

The saved branch of `RunManager.GenerateMap` calls the same hook, so the prefix does nothing when the incoming map is a `SavedActMap`. A reload never rewrites, the save needs no new data, and a save made with the mod still loads without it. The two checks answer different questions: the map's class decides whether to rewrite this map, and its shape decides whether the run is a Wayfinder run.

A run is a Wayfinder run or it isn't, and that is decided once, when the run starts:

- The rewrite reads `EnableWayfinder` only when `RunState.Map` is still `NullActMap` and `CurrentActIndex` is 0, which is a new run's first map. Flipping the toggle never affects a run in progress. The toggle's hover tip says it needs a new run.
- Every other fresh map converts only if `RunState.Map` holds a Wayfinder line. `RunState.set_Map` is called only from `GenerateMap`, so at this point the property still holds the previous act's map, or the current act's map when `GoldenCompass` regenerates mid-act.
- A Wayfinder line has one point per row, all in column 3, and an empty `startMapPoints`. The base game reads that set only in the map builders and the save code, and the save loader and the multiplayer save writer both guard against a missing list. `GoldenPathActMap` fills the set, so a vanilla golden path is not mistaken for a Wayfinder line. A reloaded map passes the same test, because `SavedActMap` restores every coord and refills the set only from a non-empty saved list.
- A run started before the mod was installed, or with the toggle off, stays vanilla for its whole length.

`SpoilsActMap` and the `StandardActMap` from `BigGameHunter` convert, because they branch. `GoldenPathActMap` keeps the 16-point order the relic promises. Its points already sit one per row in column 3, so in a Wayfinder run the rewrite only empties its `startMapPoints`, and later acts still convert.

The shuffle uses `new Rng(seed, "act_N_wayfinder")`, with `seed` from `RunState.Rng.Seed` and N the act number from 1, matching vanilla's `act_N_map`. The rewrite draws from no shared `RunRngSet` stream, so rewards and other rolls don't shift.

Act 1 without Neow keeps the vanilla Monster start, because that run has no Neow to show and keeping it needs no patch. `StartedWithNeow` comes from an unlock check when the run starts. The rewrite shuffles only the points between the start point and the boss, so the start point stays at row 0. After the map hooks run, the game changes that one point's type from Ancient to Monster, and no shuffled point is affected. That Act 1 opens with two fights, the start point and position 1, the first point after it in the ordering rules.

Multiplayer is not blocked, so co-op can be tried for testing. Both players need the same toggle setting or they generate different maps.

The `SetMap` postfix recognizes a unified path by shape: one point per row, all in column 3, and at least 26 points. It ignores the toggle, so a saved path always draws as a switchback. It can't use the map's class, because a loaded map is always a `SavedActMap`. `GoldenPathActMap` has 16 points and keeps the stock drawing.

The game behavior above comes from the v0.107.1 IL and [the lifecycle research](../assets/02-map-lifecycle-and-dependents.md). To check in-game, because the decompile can't confirm them: that the prefix fires on the call from `ModifyGeneratedMap`, that `SpoilsMap` and `FurCoat` attach to points on the finished path, and that co-op stays in step.

Excluded:

- A postfix on `Hook.AfterMapGenerated`. It can only change the map in place, and it runs after the Late hooks, so their chosen coords may no longer exist.
- A postfix on `Hook.ModifyGeneratedMap`, which has the same stale-coord problem.
- Converting the current act when the toggle flips. Reverting needs the original map, which isn't saved, and converting renumbers coords that `RemoveStaleVisitedMapCoords` then drops.
- Converting later acts of a run already in progress. Those runs need a new game.
- A hidden marker relic, which adds an item to every save, and a separate file keyed by run, which can drift from cloud saves. BaseLib's `SavedSpireField` works only on models such as cards and relics.
- Forcing an Ancient on the no-Neow Act 1, which has no Neow to show.
- Reshuffling `GoldenPathActMap`, which has nothing branching to replace.
- Reusing the `act_N_map` RNG name, which would replay the stream vanilla just used.
- Skipping the rewrite in multiplayer.
