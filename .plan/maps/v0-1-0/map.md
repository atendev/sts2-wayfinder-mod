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
  - v0.1.0 changes the order of points only, and draws that order on screen.
  - v0.1.0 targets single-player. The rewrite stays deterministic from the seeded map RNG so co-op is not blocked later.
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

## Not yet specified

- The build tickets. They depend on the decisions still open. Expect slices for the ordering engine, the map rewrite, the switchback drawing patch, the `EnableWayfinder` hover tip saying it needs a new run, and an in-game playthrough.
- How the ordering engine gets tested without the game running.
- Rewording the README and manifest, which say "a single act" where the mod means each act individually.

## Out of scope

- Tested co-op support, which is for a future version.
- Number balancing such as combat stats, gold, and rewards, which is for future versions or other mods.
