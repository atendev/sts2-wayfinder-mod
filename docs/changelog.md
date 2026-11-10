# Changelog

## v0.2.0 (unreleased)

- Wayfinder runs now scale enemies. Monster, Elite, and Unknown fights get more HP and damage along Acts 2 and 3, and bosses get a fixed boost per act. Act 1 stays vanilla. Scaled attack damage shows in the enemy's intent.
- Wayfinder treasure chests now roll between a relic chest and a gold chest. A gold chest gives no relic and twice the gold. The relic chance starts at 20% each act, rises 20% per gold chest, resets after a relic chest, and the act's last chest is always a relic chest.
- Added a difficulty settings screen under Enable Wayfinder: a preset dropdown (Off, Bosses only, Power curve, Custom) and options for enemy scaling, card rewards, treasure chests, Elite relics, and gold. Changing an option away from its preset switches the dropdown to Custom. A run copies these settings when it starts, saves them with the run, and ignores later changes. Enemy scaling and treasure chests now follow the options. The card, Elite relic, and gold options show on the screen and are saved, but have no effect yet.

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
