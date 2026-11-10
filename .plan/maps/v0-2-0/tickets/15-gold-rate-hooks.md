---
type: research
blocked_by: []
undermined_by: []
assets: [15-gold-rate-hooks.md]
---

# Find where combat, event, and chest gold is paid so a Gold rate can scale it

## Question

The settings screen has a Gold rate choice (Lean 75%, Vanilla 100%, Rich 125%) that nothing reads yet. Where does the game decide gold amounts for combat rewards (Monster, Elite, boss), events, and treasure chests, and which of those are one shared call a mod can multiply? Is there a hook, such as a `Hook.Modify*` gold method, that relics already use, and how do relics that change gold (for example Golden Idol or Ectoplasm style effects) combine with it? Which seeded RNG stream decides each amount, and does rounding after a multiplier keep runs deterministic? Does the treasure chest gold multiplier from [the treasure outcome table](./06-treasure-outcome-table.md) stack with the Gold rate or bypass it, and how are shop prices and gold gained from events affected if the hook is shared? Recommend one hook for the build ticket, with signatures checked against `references/decompiled/`.

## Answer

Every gold gain, whether combat, chest, event, or gold relic, goes through `PlayerCmd.GainGold`, which calls `Hook.ModifyGoldGained` and chains each listener's `ModifyGoldGained(Player, decimal)`. Bowler Hat's percentage is the vanilla precedent and Ectoplasm returns 0. One Harmony postfix on `Hook.ModifyGoldGained` can round `result * rate` per call, touches no RNG, and keeps Ectoplasm at zero. The chest multiplier is applied before `GainGold`, so it stacks with the Gold rate. Shop prices use `LoseGold` and don't scale. The reward screen will show unscaled combat gold, the same gap Bowler Hat has, so the build should check that in game and, if it reads as a bug, scale `GoldReward.Amount` in a `Populate` postfix instead of the hook for combat. Event gold and flat relic gold scale too, which the build ticket can narrow. The v0.111 public beta isn't decompiled, so re-check the signatures. Details: [assets/15-gold-rate-hooks.md](../assets/15-gold-rate-hooks.md).
