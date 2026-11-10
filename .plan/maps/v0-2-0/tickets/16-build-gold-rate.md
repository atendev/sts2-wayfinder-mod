---
type: task
blocked_by: [07, 11, 15]
undermined_by: []
assets: []
---

# Build the Gold rate

## Question

Build the Gold rate choice (Lean 75%, Vanilla 100%, Rich 125%) on the settings screen so it changes the gold a run pays, using the hook from [Find where combat, event, and chest gold is paid](./15-gold-rate-hooks.md): a postfix on `Hook.ModifyGoldGained` that reads the run-start snapshot from [the settings screen build](./11-build-difficulty-settings-screen.md) and rounds per call. Gated on `UnifiedActMap.IsLine` and Vanilla meaning no change. Does the reward screen's unscaled combat gold read as a bug in game, and should event gold and flat relic gold stay scaled? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.

## Answer

A postfix on `Hook.ModifyGoldGained` (`GoldRatePatch`) multiplies each gain by the run snapshot's Gold rate, 75%, 100%, or 125%, through `GoldRateScaling.Apply`, which rounds per call away from zero and leaves zero alone so Ectoplasm still blocks gold. It is gated on `UnifiedActMap.IsLine`, and Vanilla changes nothing. Event gold and flat relic gold stay scaled, since gold isn't a relic effect, and the chest multiplier stacks. The reward screen still shows a combat reward's unscaled gold, like Bowler Hat; that is documented in the changelog and left for a playtest to judge. Excluded: scaling `GoldReward.Amount` in a `Populate` postfix, which would fix the display but split the logic across two patches. The postfix takes only `runState` and `__result`, because the original's `out modifiers` parameter made a declared copy unsafe for Harmony.
