---
type: task
blocked_by: [21]
undermined_by: []
assets: [../assets/21-build-random-encounter-picks-report.md, ../assets/20-build-per-act-point-ranges-report.md]
---

# Check random encounter picks and reload in game

## Question

The `GenerateRooms` postfix from [Build random encounter picks](./21-build-random-encounter-picks.md) and the `ScaleRoomCount` list sizing from [Build per-act point ranges](./20-build-per-act-point-ranges.md) are covered by unit tests but have not run in the game. In a Wayfinder run, do an act's first Elites show each pool Elite once before any repeat, does no Elite follow itself, do no weak encounters show up late in an act, and does saving and reloading mid-act keep the same upcoming fights? Check the log for the `GenerateRooms` count-read error. This can ride along with [Playtest a Power curve run](./18-playtest-power-curve.md).
