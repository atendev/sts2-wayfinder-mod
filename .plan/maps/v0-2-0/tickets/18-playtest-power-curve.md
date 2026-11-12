---
type: task
blocked_by: [17]
undermined_by: []
assets: [../assets/00-v0-1-baseline.md]
---

# Playtest a Power curve run and compare it with the baseline

## Question

Does a Power curve run on Ironclad meet the pass criteria from [the playtest plan](./17-playtest-plan.md)? The user plays the run; the agent reads the run file and `history/wayfinder/<start_time>.json` with `scripts/run_report.py`, compares with [the v0.1 baseline](../assets/00-v0-1-baseline.md), and reports each criterion as met or missed. On a miss, adjust the tuning defaults and ask for another run, stopping after the third miss. Check against the installed v0.111 build that the patch signatures still load before the first run, and note the results of the open in-game checks: the settings screen, the run record sidecar, and the Gold rate display. Finish `docs/changelog.md` by turning the v0.2.0 entry from unreleased to final.

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
