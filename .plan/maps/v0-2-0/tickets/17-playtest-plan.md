---
type: grilling
blocked_by: []
undermined_by: []
assets: [../assets/00-v0-1-baseline.md]
---

# Set the playtest run count, characters, and pass criteria

## Question

How many Power curve runs does v0.2.0 need, on which characters, what numbers count as a pass, and how does tuning proceed when a run misses?

## Answer

One Power curve run on Ironclad, the same character as both v0.1 baseline runs, so the comparison is like for like. The run passes when all of these hold, read with `scripts/run_report.py`:

- Act 3 Elites and the Act 3 boss average 4 or more turns and 15 or more damage taken.
- 15 or fewer relics at the end of Act 1.
- Under 100 max HP gained over the whole run.
- The run is winnable: not lost or near-lost before Act 3.
- The reward screen's unscaled combat gold does not read as a bug in play.

When a run misses, adjust the tuning defaults, replay, and stop to ask the user after the third miss. Excluded: a second character, because the baseline has no Wayfinder data for the others; and never auto-tuning, since a capped loop was preferred.

The playtest also closes the open in-game checks: the settings screen, the run record sidecar, and the Gold rate display. If the gold display reads as a bug, scale `GoldReward.Amount` in a `Populate` postfix and drop combat gold from the `ModifyGoldGained` hook, not both.
