---
type: task
blocked_by: [17]
undermined_by: []
assets: [../assets/00-v0-1-baseline.md]
---

# Playtest a Power curve run and compare it with the baseline

## Question

Does a Power curve run on Ironclad meet the pass criteria from [the playtest plan](./17-playtest-plan.md)? The user plays the run; the agent reads the run file and `history/wayfinder/<start_time>.json` with `scripts/run_report.py`, compares with [the v0.1 baseline](../assets/00-v0-1-baseline.md), and reports each criterion as met or missed. On a miss, adjust the tuning defaults and ask for another run, stopping after the third miss. Check against the installed v0.111 build that the patch signatures still load before the first run, and note the results of the open in-game checks: the settings screen, the run record sidecar, and the Gold rate display. Finish `docs/changelog.md` by turning the v0.2.0 entry from unreleased to final.
