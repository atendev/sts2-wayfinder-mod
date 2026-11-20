# Changelog

## v0.2.1

- The mod's .pck no longer contains extra files. v0.2.0's .pck carried decompiled game scripts and other project files that the mod doesn't use. Gameplay is unchanged.
- Checked against the game's normal branch, version 0.111.1.
- On the game's public-beta branch, use the separate public-beta build instead (v0.2.0-beta.1 or later).

## v0.2.0-beta.1

- The first build for the game's `public-beta` branch, version 0.113.1. It has the same features as v0.2.0 and won't load on the normal branch. Use v0.2.0 there.
- Updated Wayfinder's game hooks for 0.113.1, which renamed or reshaped several game methods the mod changes. Without this the mod failed to load on the beta.
- The switchback map follows the beta's new map screen layout.

## v0.2.0

- Wayfinder runs now scale enemies. Monster, Elite, and Unknown fights get more HP and damage along Acts 2 and 3, and bosses get a fixed boost per act. Act 1 stays vanilla. Scaled attack damage shows in the enemy's intent.
- Wayfinder treasure chests now roll between a relic chest and a gold chest. A gold chest gives no relic and twice the gold. The relic chance starts at 20% each act, rises 20% per gold chest, resets after a relic chest, and the act's last chest is always a relic chest.
- Added a difficulty settings screen under Enable Wayfinder: a preset dropdown (Off, Bosses only, Power curve, Custom) and options for enemy scaling, card rewards, treasure chests, Elite relics, and gold. Changing an option away from its preset switches the dropdown to Custom. A run copies these settings when it starts, saves them with the run, and ignores later changes. Enemy scaling and treasure chests now follow the options.
- Wayfinder card rewards now follow the card options. With Alternate card rewards on, normal combats, including combats from Unknowns, give a card reward on every other fight, starting with each act's first. Gold and potions still drop. Elites and bosses always give one. Card choices sets how many cards a combat's reward offers, Elites and bosses included, 4 in Power curve. Card rewards from relics such as Prayer Wheel, and from events, are unchanged.
- Wayfinder Elites can now drop lower-rarity relics. With Lower Elite relic rarity on, an Elite's relic is Common 75%, Uncommon 20%, and Rare 5% of the time, against vanilla's 50%, 33%, and 17%. Elites still always drop one. Black Star's extra relic, events, and shops keep vanilla rarity.
- Wayfinder runs now write a difficulty record when they end, at `history/wayfinder/<start_time>.json` next to the run history. It holds the preset, every tuning value, a schema version, and the mod version. `scripts/run_report.py` reads it. A run abandoned from the main menu gets no record. Nothing changes in the `.run` file or on screen.
- Wayfinder gold now follows the Gold rate option: Lean pays 75%, Vanilla 100%, Rich 125%. It scales every gold gain, including combat, events, chests, and gold relics, rounded per gain, and stacks with a gold chest's multiplier. Shop prices don't change. A combat reward is scaled when it is rolled, so the reward screen shows the gold you get.
- Added a Path length setting: Acts, the default in every preset, or Full map. With Acts, the run seed rolls each act's length and number of each point type from its own ranges, and the vanilla map only supplies the start point and the boss. Act 1 (Build the deck) is 34 to 38 points, Act 2 (The wilds) 32 to 36, and Act 3 (The gauntlet) 26 to 30, about 96 points a run against 157. Full map keeps every generated point, as in v0.1. The setting is saved with the run and written to the difficulty record.
- Each act now places its Elites in its own way. Act 1 has no Elite in the first third and at least 5 points between Elites. Act 2 allows Unknown runs of 2 to 3. Act 3 allows an Elite pair with a rest site right before it, never three in a row, and no other Elite within 4 points either side.
- With the Acts path length, each act's normal encounter list is sized to that act's longest path instead of doubled. The weak encounter draw still doubles.
- Lowered the path enemy scaling curve to +30% HP and +20% damage at the end of Act 2, and +50% HP and +50% damage at the end of Act 3, since the path is shorter and playtest Act 3 fights, Knights especially, hit too hard. Boss damage is a little lower, +10% in Act 2 and +20% in Act 3, and boss HP stays as it was. The hover tips for the two path scaling options now show these numbers.
- The mod description now names each act's identity.
- Wayfinder fights now stay random after the game's encounter lists run out. Each act's first Elites are every Elite in its pool once, in random order, then each Elite is a random pick that skips the one you just fought. Once an act's normal fights pass the game's list, each one is a random regular encounter that skips the previous one, so weak encounters don't return late in the act. Combats from Unknowns share the normal fight count. Each act's lists are drawn when its rooms are generated at run start and saved with the run, so a reload keeps the same fights. They hold 48 Elites and 96 normal fights per act, far more than an act reaches.

## v0.1.1

- Added an MIT license. The release zip now includes the license, README, and changelog.
- The mod now runs on both the stable and public-beta game builds. Starting a run on public-beta no longer fails with a missing `RunRngSet.get_Seed()` method.
- The unified path shuffle now derives from the run's seed text, so a given seed produces a different path than it did in v0.1.0.

## v0.1.0

- Added the ordering engine, which turns an act's mix of points and a seeded RNG into one ordered path that the game's map uses.
- Added an xUnit test project for the ordering rules under `tests/OrderingTests`. It runs without the game.
- Each act's branching map is now rewritten into one unified path when a Wayfinder run generates it.
- The map screen now draws a unified path of 26 or more points as a switchback. Shorter paths keep the stock single column.
- A Wayfinder run now draws twice as many weak encounters per act, and each act's normal encounter list is twice the room count.
- On a Wayfinder run, relics limited to before the Act 3 chest now stop appearing when Act 3 starts, instead of at a fixed floor.
- The Enable Wayfinder hover tip now says the setting takes effect on a new run.
- The README and mod manifest now say each act gets its own unified path, instead of "a single act".
