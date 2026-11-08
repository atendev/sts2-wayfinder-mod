## Destination

Wayfinder v0.1.0 ships: the mod builds, loads in Slay the Spire 2, and replaces every act's branching map with one reshuffled unified path. A full act has been played in-game from the Ancient to the boss, and `docs/changelog.md` has a v0.1.0 entry.

## Notes

- This map overrides wayfinder's plan-only default. It starts with decision tickets and moves on to build tickets once the decisions are in. Later versions get their own maps.
- Vocabulary lives in `CONTEXT.md`. Use "point" for any stop on the map and keep "encounter" for combats.
- Settled while charting:
  - Every act is rewritten on its own. Each act still starts at its start point and ends at its boss, and only the points between them change. The start point is the act's Ancient, except a Monster in Act 1 when the run did not start with Neow.
  - A `GoldenCompass` act keeps its 16-point `GoldenPathActMap`, which is already a strict line in a fixed order. Every act that branches is rewritten.
  - The unified path is a strict line and the player never chooses a direction.
  - Every point the vanilla map generated goes on the path, of all types. It is a reshuffle, not the horizontal joining of existing paths that earlier iterations of this mod used. The goal is to avoid sequences like two elites back to back or five chests in a row.
  - v0.1.0 changes the order of points and draws that order on screen. The only exceptions are the encounter list and relic check in [the content decision](./tickets/09-content-built-for-shorter-acts.md).
  - v0.1.0 targets single-player. The rewrite stays deterministic from the seeded map RNG so co-op is not blocked later. Multiplayer stays untested. The mod adds nothing to protect it or block it, and changes that affect it by proxy are fine.
- Game facts come from `references/decompiled/` (gitignored), generated from the installed v0.107.1 `sts2.dll`: `sts2.txt` has signatures and calls, `sts2-il.txt` has full IL. Downloading a decompiler was refused in this environment, so don't retry it. Regenerate the files with a local metadata dumper if the game updates.
- `FightAllChoices` in `references/reference-mods/` shows a working technique but ships no license. Reuse its approach, not its code.
- Record each mod change in `docs/changelog.md` as build tickets land.

## Decisions so far

- [Measure what a vanilla act generates](./tickets/01-vanilla-act-point-counts.md): 13 to 15 rows on a 7-wide grid with fixed Monster, Treasure, and RestSite rows, and roughly 57 to 66 points before pruning (inferred).
- [Trace the map lifecycle and what depends on map shape](./tickets/02-map-lifecycle-and-dependents.md): `AfterMapGenerated` also fires on load, some relics replace the map early, and history is indexed by row.
- [Trace how the map screen draws and travels](./tickets/03-map-screen-and-travel.md): any grid size saves and draws, spacing and scroll limits are hard-coded, and a line of single children needs no travel patch.
- [Find how content pools behave on a longer act](./tickets/04-content-pools-on-long-path.md): pools wrap and never throw, so weak encounters return on a second pass.
- [Choose how the unified path is laid out on screen](./tickets/05-path-layout-on-screen.md): a switchback drawn by one `SetMap` postfix over one-point-per-row data, from the Ancient at bottom center to the boss at top center, with stock drawing below 26 points.
- [Define the ordering rules](./tickets/06-ordering-rules.md): hard spacing rules with a fight first and a rest last, Elites, Rest sites, and Shops spread evenly with seeded jitter, chests random and capped at 3 in a row, and a fixed break order so ordering never fails.
- [Decide where the rewrite happens and how saves behave](./tickets/07-rewrite-integration.md): a prefix on `ModifyGeneratedMapLate` that skips saved maps, seeded by `act_N_wayfinder`, with the toggle read only when a run starts and in-progress runs left vanilla.
- [Preview sample paths under the ordering rules](./tickets/08-preview-sample-paths.md): the rules read as balanced for every act, with one added preference capping Monsters and Elites at 4 in a row, which gives way before any other preference.
- [Decide how to handle content built for shorter acts](./tickets/09-content-built-for-shorter-acts.md): Wayfinder runs double weak encounters and make the normal list twice the room count, the Act 3 chest relic check turns off when Act 3 starts, and other floor checks stay as they are.
- [Decide how the ordering engine gets tested without the game](./tickets/10-test-ordering-engine.md): the engine sits in a game-free folder linked into an xUnit project, takes randomness through one `NextInt` method, and returns the rules it broke so seven required checks can run on fixed seeds.
- [Build the ordering engine](./tickets/11-build-ordering-engine.md): built in `Ordering/` with integer-only randomness and one Elite gap step, with all seven checks passing and no connection to the game yet.
- [Rewrite each act's map into a unified path](./tickets/12-rewrite-map.md): a Harmony prefix swaps each fresh map in a Wayfinder run for a `UnifiedActMap` shaped like the golden path, verified by build and harness but not yet in-game.
- [Draw the unified path as a switchback](./tickets/13-draw-switchback.md): a `SetMap` postfix moves points into ranks from a game-free layout and redraws every link, with stock drawing below 26 points, verified by build, tests, and reflection but not yet in-game.
- [Extend the normal encounter list on Wayfinder runs](./tickets/14-extend-encounter-list.md): a transpiler on `GenerateRooms` doubles the weak draws and the list length when the toggle is on, verified by build and against the real IL but not yet applied in-game.
- [Turn the Act 3 chest relic check off when Act 3 starts](./tickets/15-act-3-relic-check.md): a postfix returns `CurrentActIndex < 2` on a unified path and leaves other runs vanilla, verified by build and the IL dump but not yet applied in-game.
- [Say on the EnableWayfinder hover tip that it needs a new run](./tickets/16-toggle-hover-tip.md): the existing `.hover.desc` text now says the toggle takes effect on a new run and leaves a run in progress alone, verified by JSON parse but not yet seen in-game.
- [Reword the README and manifest for each act](./tickets/17-reword-readme-manifest.md): both now say every point in each act goes into one unified path for that act.

## Not yet specified

Nothing. Every remaining slice has a ticket.

## Out of scope

- Tested co-op support, which is for a future version.
- Number balancing such as combat stats, gold, and rewards, which is for future versions or other mods.
