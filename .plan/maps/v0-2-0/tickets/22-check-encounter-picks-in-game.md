---
type: task
blocked_by: [21]
undermined_by: []
assets: [../assets/21-build-random-encounter-picks-report.md, ../assets/20-build-per-act-point-ranges-report.md]
---

# Check random encounter picks and reload in game

## Question

The `GenerateRooms` postfix from [Build random encounter picks](./21-build-random-encounter-picks.md) and the `ScaleRoomCount` list sizing from [Build per-act point ranges](./20-build-per-act-point-ranges.md) are covered by unit tests but have not run in the game. In a Wayfinder run, do an act's first Elites show each pool Elite once before any repeat, does no Elite follow itself, do no weak encounters show up late in an act, and does saving and reloading mid-act keep the same upcoming fights? Check the log for the `GenerateRooms` count-read error. This can ride along with [Playtest a Power curve run](./18-playtest-power-curve.md).

## Findings so far (2026-11-20)

Checked against runs 4 and 5 (1795021557 and 1795070129) from `map_point_history` and the game logs.

- Each pool Elite before any repeat: met in all six acts. Act 1 and Act 2 of run 5 had three Elites, all different. Act 3 of run 5 had six: Mecha Knight, Knights, Soul Nexus, then repeats. Run 4 had four per act, with the fourth repeating one of the first three.
- No Elite follows itself: met in every act.
- No weak encounters late: met. Each act's weak fights come first (4 to 6), and none appear after the first normal fight.
- No normal encounter follows itself: met.
- `GenerateRooms` count-read error: none. The one `error` hit in the 11-18 log is a BaseLib config warning, and the 11-20 logs are clean.
- Save and reload mid-act: not confirmed. The run files and logs don't record a mid-act reload, so this needs one deliberate test.
