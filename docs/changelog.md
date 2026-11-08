# Changelog

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
