---
type: task
blocked_by: [17, 20, 21]
undermined_by: []
assets: [../assets/00-v0-1-baseline.md]
---

# Playtest a Power curve run and compare it with the baseline

## Question

Does a Power curve run on Ironclad meet the pass criteria from [the playtest plan](./17-playtest-plan.md)? The user plays the run; the agent reads the run file and `history/wayfinder/<start_time>.json` with `scripts/run_report.py`, compares with [the v0.1 baseline](../assets/00-v0-1-baseline.md), and reports each criterion as met or missed. On a miss, adjust the tuning defaults and ask for another run, stopping after the third miss. Check against the installed v0.111 build that the patch signatures still load before the first run, and note the results of the open in-game checks: the settings screen, the run record sidecar, and the Gold rate display. Finish `docs/changelog.md` by turning the v0.2.0 entry from unreleased to final.

Criteria update (2026-11-18): [per-act point ranges](./20-build-per-act-point-ranges.md) cut a run from about 157 points to about 96 and Act 1 from about 58 to 34 to 38, so two limits from the playtest plan scale with point count. A run now passes with at most 10 relics at the end of Act 1 (was 15) and under 60 max HP gained (was 100). The Act 3 Elite and boss targets of 4+ turns and 15+ damage are per fight and stay. Run 1 below was scored against the old limits on the full path.

## Run 1 (miss 1 of 3)

Run 1794377855, Ironclad, Ascension 0, won, build v0.107.1 (the installed build; the decompile matches it). Config at the time read Power curve with Balanced chests, 4 card choices, and Vanilla gold. Playtime is ignored, since the run spanned several sessions.

| Criterion | Result |
| --- | --- |
| Act 3 Elites and boss, 4+ turns and 15+ damage | Turns missed: 3.5 over the 6 fights (Elites 3.2, boss 5). Damage met: 21.3 (Elites 22.6, boss 15). |
| 15 or fewer relics at the end of Act 1 | Met: 13 (baseline 16 to 22). |
| Under 100 max HP gained | Met: 92 (baseline 144 to 218). |
| Winnable | Met: lowest HP was 38 in Act 1 and the run won. |
| Gold display not a bug | Open: not noticed, so unchecked. |

Act 3 Monster fights averaged 10.4 damage and 2.5 turns, against 1.3 to 1.9 damage and 1.4 to 1.5 turns in the baseline. Final relics were 55 and the final deck 51.

In-game checks:

- Settings screen: not tested. The config file saved all 10 values, and the log warns that every option's `hover.desc` key is missing from `settings_ui.json`, so no hover tips show.
- Run record sidecar: written, but to the wrong folder. `RunRecordPatch` built its path from a relative `GetHistoryPath`, so the file landed in the game's install folder under `modded/profile1/saves/history/wayfinder/` instead of the save folder. Run 1 (a win) and the abandoned test run both got one, so the write path and `IsLine` work at run end. The path now comes from `UserDataPathProvider.GetProfileScopedPath`.
- Patch signatures: the mod loaded cleanly on later launches. The first launch threw in `TreasureRoomRelicSynchronizer.BeginRelicPicking`, which is the underscored-field fix already committed.

Tuning change: end-of-Act-3 path HP 290% to 360%, to lift Act 3 turns from 3.5 toward 4. Damage is already well over target, so path damage stays. Run 2 is next.

Run 2 checks, before the run: the 360% build loaded cleanly on v0.107.1, and the settings screen works, with the preset dropdown and sections showing and a hand edit flipping the preset to Custom.

Checks on the fixed build, from a short throwaway run with Gold rate set to Rich (recorded as Custom):

- Gold display: passed. The reward screen amounts (25, 25, 23, 14, 19) matched the gold paid in the run file, so the `Populate` postfix and the hook skip work together with no double scaling.
- Run record sidecar: passed. `history/wayfinder/1794493392.json` appeared in the save folder on abandon, with the preset Custom and Gold rate Rich.

Still open: run 2 itself, for the Act 3 turns criterion.

## Run 2 (not scored)

Run 1794493783 was played on Silent, which the playtest plan excludes (the baseline has Ironclad only). Act 3 Elites and boss averaged 2.3 turns and 4.7 damage. Not counted as a miss.

## Run 3 (miss 2 of 3)

Run 1794829802, Ironclad, Ascension 0, won, build v0.107.1 with end-of-Act-3 path HP at 360%. The sidecar reads Power curve, Balanced chests, 4 card choices, Vanilla gold.

| Criterion | Result |
| --- | --- |
| Act 3 Elites and boss, 4+ turns and 15+ damage | Turns missed: 2.0 over the 6 fights (Elites 1, 1, 1, 3, 2; boss 4). Damage met: 22.7 (Elites 7, 13, 18, 24, 29; boss 45). |
| 15 or fewer relics at the end of Act 1 | Missed: 18 (run 1 had 13). |
| Under 100 max HP gained | Met: 36. |
| Winnable | Met: lowest HP was 52 in Act 1 and the run won. |
| Gold display not a bug | Met: passed in the earlier throwaway run. |

The run ended with 63 relics and a 71-card deck, so Act 3 Elites died in 1 to 2 turns despite the 360% end value. Run 1 reached 3.5 turns at 290%, so turns vary more by run than by the curve.

Tuning change: end-of-Act-3 path HP 360% to 500%, to lift Act 3 turns toward 4. Path damage stays, since damage is already over target. The Act 1 relic miss is left alone for now: it sits 5 above run 1's count, and the curve doesn't affect it. If run 4 misses it again, that points at Elite, Unknown, or shop relic sources.

## HP rollback

The user pulled end-of-Act-3 path HP back from 500% to 300% before run 4. Raising HP kept chasing player power (run 3 ended with 63 relics and a 71-card deck), so the next tuning step looks at other levers instead of more HP.

[Per-act point ranges](./19-per-act-point-ranges.md) then shortened the path to about 96 points a run and dropped the path curve to 130% / 150% HP and 130% / 180% damage at the end of Act 2 / Act 3. Run 4 waits on [the ranges build](./20-build-per-act-point-ranges.md) and [the encounter picks build](./21-build-random-encounter-picks.md). Re-check the pass criteria against the shorter path before scoring it, since the Act 1 relic limit of 15 was set for a 58-point Act 1.

## Handoff for the next session

State: miss 2 of 3 is logged. The curve is at 130% / 150% HP and 130% / 180% damage. Run 4 is blocked on tickets 20 and 21.

To score the next run:

1. Find the newest `.run` in `~/.local/share/SlayTheSpire2/steam/<steamid>/modded/profile1/saves/history/`. Ignore throwaway runs `1794493048` and `1794493392`, and the Silent run `1794493783`. Skip `.run.backup` files.
2. Run `scripts/run_report.py` on it and pull Act 3 Elite and boss turns and damage straight from `map_point_history`, pooled as in runs 1 and 3.
3. Check each criterion from [the playtest plan](./17-playtest-plan.md), including Act 1 relics.
4. Pass: add `## Answer`, add the Decisions bullet to `map.md`, and change the `docs/changelog.md` v0.2.0 heading from `(unreleased)` to final.
5. Miss: log it as miss 3, then stop and ask the user.
