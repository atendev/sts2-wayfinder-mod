# Changelog

## v0.1.0 (in progress)

- Added the ordering engine, which turns an act's mix of points and a seeded RNG into one ordered path. It isn't connected to the game's map yet.
- Added an xUnit test project for the ordering rules under `tests/OrderingTests`. It runs without the game.
- Each act's branching map is now rewritten into one unified path when a Wayfinder run generates it.
- The map screen now draws a unified path of 26 or more points as a switchback. Shorter paths keep the stock single column.
- A Wayfinder run now draws twice as many weak encounters per act, and each act's normal encounter list is twice the room count.
- On a Wayfinder run, relics limited to before the Act 3 chest now stop appearing when Act 3 starts, instead of at a fixed floor.
- The Enable Wayfinder hover tip now says the setting takes effect on a new run.
