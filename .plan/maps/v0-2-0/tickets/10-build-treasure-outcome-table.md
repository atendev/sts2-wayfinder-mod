---
type: task
blocked_by: [06, 07]
undermined_by: []
assets: []
---

# Build the treasure outcome table

## Question

Build the chest table decided in [Decide the treasure outcome table](./06-treasure-outcome-table.md), using the hooks from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md). Roll the outcome in `TreasureRoomRelicSynchronizer.BeginRelicPicking` on the `TreasureRoomRelics` stream, and only inside the `Hook.ShouldGenerateTreasure` branch so Silver Crucible's chest gets no roll. Apply the gold chest multiplier in `OneOffSynchronizer.DoTreasureRoomRewards` while still drawing vanilla's `NextInt(42, 53)` so the `PlayerRng.Rewards` counter stays aligned. Gate everything on `UnifiedActMap.IsLine` and the settings read at run start. The settings are now a "Use treasure chest chances" toggle plus a Chest odds choice (Generous 25%/+25%/2x, Balanced 20%/+20%/2x, Stingy 15%/+15%/1.5x), per [the settings screen](./07-difficulty-settings-screen.md); with the toggle off, chests stay vanilla. How does the patch find the act's last treasure point and count misses since the last relic chest from the map point history, so a save and reload gives the same chance? Re-check the listed signatures against the installed build. Record the change in `docs/changelog.md`.

## Answer

Built in `sts2_wayfinder_modCode/Difficulty/`: `TreasureChestTable` (the chance, miss, and forced-last-chest rules, plus the Generous, Balanced, and Stingy `TreasureOdds` presets), `TreasureChestPatch` (the game patches), and a `Treasure` property on `DifficultySettings`. `TreasureChestTableTests` covers the rules. The mod builds against the installed `sts2.dll` and the 52 tests pass. Nothing was run in the game.

- **Roll:** a prefix on `TreasureRoomRelicSynchronizer.BeginRelicPicking` draws one `NextFloat()` from the run's `TreasureRoomRelics` stream, only when some player's `Hook.ShouldGenerateTreasure` is true, so Silver Crucible's chest gets no roll and no draw.
- **Gold chest, relic side:** the prefix sets a flag, and a postfix on `Hook.ShouldGenerateTreasure` returns false while it is set. The game then takes its own empty-chest path: no relic pulled from the grab bag, no rarity draw, empty-chest VFX. A finalizer clears the flag.
- **Gold chest, gold side:** a prefix on `OneOffSynchronizer.DoTreasureRoomRewards` replaces the method for a rolled gold chest. It still draws `NextInt(42, 53)`, applies the multiplier then the Poverty 0.75, pays through `PlayerCmd.GainGold`, and calls the private `TryHandleSpoilsMap`. A relic chest runs vanilla untouched. The rolled outcome sits in a static keyed on act and map coord, set on each room entry.
- **Reload:** earlier chests are rebuilt from `MapPointHistory` for the current act, so chance and misses need no save data. A chest with a relic in `RelicChoices` is a relic chest, one with only gold is a gold chest, and one with neither (Silver Crucible) is skipped. A reload inside a chest room re-enters the room, and the restored stream counter gives the same roll.
- **Last chest:** the act's last treasure point is the one with the highest row among the map's Treasure points, and it is always a relic chest.
- **Settings:** `DifficultySettings.Treasure` is a static defaulting to Balanced (20%, +20%, 2x), like the enemy scaling toggles. [Build the difficulty settings screen](./11-build-difficulty-settings-screen.md) replaces it with the run-start snapshot and the toggle and Chest odds choice.

Excluded: a stored outcome per chest (history already holds it), a transpiler on the async gold method, and rolling on `PlayerRng.Rewards`.

Not verified: that a picked treasure relic lands in the chest's `RelicChoices` in the installed build, so that a relic chest isn't miscounted as a miss. A chest whose relic the player skips reads as a gold chest and adds a step. Gold rate isn't applied yet because that build has no ticket. The signatures match the installed `sts2.txt` decompile. The playtest ticket covers the in-game behavior.
