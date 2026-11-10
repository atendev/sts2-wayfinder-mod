---
type: task
blocked_by: [03, 07]
undermined_by: []
assets: []
---

# Build lower Elite relic rarity

## Question

Build the Lower Elite relic rarity toggle, using the hook from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md): swap in a `RelicReward` with an explicit rarity so an Elite's relic shifts toward Common while it still always drops one. Gated on `UnifiedActMap.IsLine` and the settings read at run start, per [the settings screen](./07-difficulty-settings-screen.md). How far does the shift go, and does it keep the seeded reward RNG counter aligned? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.

## Answer

`Difficulty/EliteRelicPatch.cs` postfixes the private `RewardsSet.GenerateRewardsFor(Player, AbstractRoom)`. On an Elite room of a `UnifiedActMap.IsLine` map with `LowerEliteRelicRarity` set in the run snapshot, it replaces the vanilla `RelicReward(Player)` with `new RelicReward(RelicRarity, Player)`. The rarity comes from one `player.PlayerRng.Rewards.NextFloat()` mapped by `EliteRelicRarity.Tier`: under 0.75 Common, under 0.95 Uncommon, else Rare. That is 75/20/5 against vanilla's 50/33/17, so Elites shift toward Common without ever becoming certain Common.

Vanilla makes the same single draw from that stream inside `RelicReward.Populate`. Populate with an explicit rarity draws nothing, so the stream's draw count is unchanged and a save and reload replays it. The draw now happens before the gold and card rewards populate rather than after, which only reorders which values those rewards see. Signatures checked against `references/decompiled/sts2.txt`: `RelicReward..ctor(RelicRarity, Player)`, `RelicFactory.PullNextRelicFromFront(Player, RelicRarity)`, and `Rng.NextFloat()`.

Excluded: patching `RelicFactory.RollRarity`, which would shift shops, events, and chests, and Black Star's extra Elite relic, since individual relic effects stay vanilla. Still open: a playtest of Elite relic spread, and whether the Common pool runs dry in a long run and falls through to Uncommon. `tests/OrderingTests/EliteRelicRarityTests.cs` covers the thresholds.
