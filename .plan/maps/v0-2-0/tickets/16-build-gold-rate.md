---
type: task
blocked_by: [07, 11, 15]
undermined_by: []
assets: []
---

# Build the Gold rate

## Question

Build the Gold rate choice (Lean 75%, Vanilla 100%, Rich 125%) on the settings screen so it changes the gold a run pays, using the hook from [Find where combat, event, and chest gold is paid](./15-gold-rate-hooks.md): a postfix on `Hook.ModifyGoldGained` that reads the run-start snapshot from [the settings screen build](./11-build-difficulty-settings-screen.md) and rounds per call. Gated on `UnifiedActMap.IsLine` and Vanilla meaning no change. Does the reward screen's unscaled combat gold read as a bug in game, and should event gold and flat relic gold stay scaled? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.
