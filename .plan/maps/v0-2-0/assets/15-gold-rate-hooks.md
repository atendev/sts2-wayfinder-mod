# Where gold is paid and how a Gold rate can scale it

Source: `references/decompiled/sts2.txt` (call lists, not source). Signatures match the installed `sts2.dll`; the v0.111 public beta is not decompiled, so re-check before building.

## One shared choke point

Every gold gain goes through `PlayerCmd.GainGold(decimal amount, Player player, bool wasStolenBack)`. It calls `Hook.ModifyGoldGained(runState, combatState, amount, player, out modifiers)`, which iterates every hook listener's `AbstractModel.ModifyGoldGained(Player, decimal)` and chains the result, then `Hook.AfterModifyingGoldGained`, then adds to `Player.Gold` and records `GoldGained` in the map point history.

Callers of `GainGold` (28):

- Combat gold: `GoldReward.OnSelect`, with the amount rolled earlier.
- Treasure chests: `OneOffSynchronizer.DoTreasureRoomRewards`, which rolls `PlayerRng.Rewards` (42 to 52) and applies the Poverty ascension multiplier before calling `GainGold`.
- Events: Trial, TrashHeap, ThisOrThat, TheLanternKey, SunkenTreasury, SunkenStatue, LostWisp, JungleMazeAdventure, EndlessConveyor, DenseVegetation, ColossalFlower.
- Relics and cards: SignetRing, OldCoin, MawBank, LuckyFysh, GoldenPearl, CursedPearl, FoulPotion, SpoilsMap, HandOfGreed.
- Not gains: `LoseGold` (shop purchases, thefts) and the dev console `SetGold`.

## Combat gold amount

`GoldReward` has `_min`/`_max` (defaults 0 and 20 constants, encounters override through `EncounterModel.MinGoldReward` and `MaxGoldReward`). `Populate()` rolls `Rng.NextInt(min, max)` on `PlayerRng.Rewards` (or the reward's `_rngOverride`) and stores `Amount`, which the reward screen shows. `OnSelect` then calls `GainGold(Amount)`, so `ModifyGoldGained` changes the paid amount without changing the shown one.

## Existing listeners

- `BowlerHat.ModifyGoldGained` multiplies by its `GoldIncrease` dynamic var. That is the vanilla precedent for a percentage on all gold, and it has the same shown versus paid gap.
- `Ectoplasm.ModifyGoldGained` returns 0, so no gold at all. A postfix on the shared hook keeps zero at zero.

## Recommendation

One Harmony postfix on `Hook.ModifyGoldGained`, in the style of the damage postfix: read the run-start snapshot, return `Math.Round(result * rate, MidpointRounding.AwayFromZero)`, and skip when the preset's Gold rate is Vanilla. It covers combat, chests, events, and gold relics with one patch. No RNG is touched, so streams and determinism stay as they are, and rounding per call keeps the paid amount a whole number.

- **Chests:** the 2x or 1.5x chest multiplier in the `DoTreasureRoomRewards` prefix happens before `GainGold`, so Gold rate stacks on top. At Lean (75%) a Balanced gold chest pays 1.5x vanilla; at Rich it pays 2.5x. That is intended unless playtests say otherwise.
- **Events and relics:** event gold, Old Coin, Hand of Greed and the rest scale too. The map's settled note says event relics and individual relic effects stay vanilla, and a flat gold payout from an event or Old Coin is gold, not a relic effect, so scaling it is consistent with a gold rate. Say so in the build ticket if this should be narrowed.
- **Shops:** purchase prices use `LoseGold`, so they don't scale. Lean makes shops relatively dearer, which matches the intent.
- **Display gap:** the reward screen will show the unscaled combat gold. The build ticket should check in game and, if it reads as a bug, scale `GoldReward.Amount` in a `Populate` postfix and exclude combat `GoldReward` from the hook postfix, not both.
