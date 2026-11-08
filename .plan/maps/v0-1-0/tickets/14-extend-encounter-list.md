---
type: task
blocked_by: [09, 12]
undermined_by: []
assets: []
---

# Extend the normal encounter list on Wayfinder runs

## Question

Change `ActModel.GenerateRooms` as [the content decision](./09-content-built-for-shorter-acts.md) describes. Double the weak encounters and make the list twice `GetNumberOfRooms`, only inside that method, drawing from the existing grab bag. The patch reads `EnableWayfinder` itself, so it needs no setup patch. Confirm that read agrees with the read for the first map.

## Answer

`ExtendEncounterListPatch` is a Harmony transpiler on `ActModel.GenerateRooms`. It adds a scale call after the method's three count reads: the weak loop bound, the regular loop start, and the regular loop bound. With `EnableWayfinder` on, each value doubles, so weak encounters take the first slots and the list is twice `GetNumberOfRooms`. `StandardActMap` still reads the vanilla room count, so the map is unchanged. Draws come from the grab bags the method already uses.

If a game update changes the number of reads from 3, the patch logs an error and leaves the method unchanged. A throw during `PatchAll` could leave the rest of the mod unpatched.

On a new run the toggle read agrees with the read for the first map as far as traced. `NGame.StartNewSingleplayerRun` calls `SetUpNewSingleplayer`, then `StartRun`, with no await between them. The trace stopped at `StartRun`, so the path from there to the first map is unconfirmed.

Every shipped act has at most 3 weak encounters and at least 13 rooms, so the list length holds without a clamp. An act with more weak draws than rooms would produce only weak entries, as vanilla does.

Accepted edges:

- A save with no map yet in act 0, reloaded after the toggle flips, could pair a list built under one toggle value with a map built under the other. [The rewrite decision](./07-rewrite-integration.md) ruled out storing a run flag.
- The debug `ActConsoleCmd` also calls `GenerateRooms`, so a console act swap follows the current toggle, even mid-run.

Verified by build, the 21 ordering tests, and a scratch run of the transpiler against the real `sts2.dll`, which placed the scale call after all 3 reads. The scale method gave 3 to 6 with the toggle on and 3 to 3 with it off. Applying the patch live failed in that scratch run because this machine has only .NET 8 and 10 and Harmony rejected 10, so [the playthrough](./18-playthrough.md) has to confirm it in-game.

Excluded:

- A global patch on `GetNumberOfRooms`, which would change the map's row count.
- A postfix that rebuilds the list, which would lose the grab bag state.
