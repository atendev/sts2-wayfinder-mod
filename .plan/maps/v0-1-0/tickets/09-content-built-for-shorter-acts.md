---
type: grilling
blocked_by: [01, 02, 04, 07]
undermined_by: []
assets: []
---

# Decide how to handle content built for shorter acts

## Question

An act's normal encounter list has 13 to 15 entries and wraps when exhausted, so a path with more combats replays them, and the weak opening encounters return mid-act. Is that acceptable for v0.1.0, or should the mod change how the list is consumed? Floor checks also shift on a longer path: `EncounterModel.GenerateMonstersWithSlots`, the `PunchOff` and `SlipperyBridge` conditions, and `RelicModel.IsBeforeAct3TreasureChest` all read `RunState.TotalFloor`. Should the mod leave those alone? The map's scope limits v0.1.0 to ordering changes, so any change here needs to be justified against that limit.

## Answer

Wayfinder runs change two things beyond the order of points: how each act's normal encounter list is built, and when one relic check turns off. Both are exceptions to the map's ordering-only limit. The list change gives the player more easy combats at the start of a much longer act. The relic check restores behavior that the longer path would otherwise break.

The normal encounter list changes as follows:

- The number of weak encounters each act draws (`NumberOfWeakEncounters`) doubles. Per [the content pools research](../assets/04-content-pools-on-long-path.md), Overgrowth and Underdocks go from 3 draws to 6, and Hive and Glory go from 2 draws to 4.
- The list length becomes twice `GetNumberOfRooms`, so 26 to 30 entries in single-player and 24 to 28 in multiplayer, where the room count is 1 lower. The change reaches multiplayer by proxy and is untested there, as the map's notes allow. Weak encounters take the first slots and regular encounters fill the rest.
- Weak encounters draw from the single grab bag `GenerateRooms` already uses. `GrabAndRemove` empties the bag and refills it only when it is empty, so every weak encounter in the act appears once before any repeats. Draws and pool size are separate numbers. The game defines 15 weak encounters across its 4 acts, so an act's pool holds about 3 or 4 of them, and some repeat within a block of 4 to 6 draws. The dump does not show how the 15 split across acts.
- Doubling applies only inside `ActModel.GenerateRooms`. `StandardActMap` also reads `GetNumberOfRooms` for the row count, so a global patch would change the map itself.

`RunManager.GenerateRooms` runs only from `SetUpNewSingleplayer` and `SetUpNewMultiplayer`, for every act at once, before any map exists. The list and its visit counter are saved as `NormalEncounterIds` and `NormalEncountersVisited`, so reloads need no patch and no save changes. A map's shape can't identify a Wayfinder run this early, so the patch on `ActModel.GenerateRooms` reads `EnableWayfinder` itself, and no separate setup patch is needed. An act with more combats than the list holds still wraps and replays from the top, which includes the weak encounters. The modeled mixes average 16 to 25 Monster points per act plus Unknowns that roll Monster, so only unusual acts reach that.

`RelicModel.IsBeforeAct3TreasureChest` is `TotalFloor < 41` in single-player and `< 38` in multiplayer. Vanilla floor 41 falls in Act 3, but on a Wayfinder path its 17 relic users would stop appearing about two-thirds of the way through Act 1. On a Wayfinder run the check becomes `CurrentActIndex < 2`, which turns it off when Act 3 starts. The index counts from 0, as [the rewrite decision](./07-rewrite-integration.md) uses it for a run's first map. The new check replaces both the single-player and multiplayer thresholds. That is a little earlier than vanilla's chest, and it is fine because the player is already strong by Act 3 on a path this long. A run counts as a Wayfinder run here when `RunState.Map` is a Wayfinder line, as [the rewrite decision](./07-rewrite-integration.md) defines it: one point per row, all in column 3, with an empty `startMapPoints`. A vanilla `GoldenPathActMap` fills `startMapPoints`, so it does not match. The relic check runs after a map exists and can use the shape, while the list is built before any map exists and has only the toggle. That decision also ruled out storing a run flag in the save. Other runs keep the vanilla check.

The other `TotalFloor` readers stay unpatched. `PunchOff` needs `TotalFloor >= 6` and `SlipperyBridge` needs `TotalFloor > 6`, and those points come at the same count on any path. `GenerateMonstersWithSlots` only adds `TotalFloor` to a seed, so its rolls stay deterministic.

Excluded:

- Keeping the list at 13 to 15 entries, or adding only the extra weak slots. Both wrap in most acts and bring 4 to 6 weak encounters back mid-act.
- Capping weak encounters at the act's pool size. Repeats are fine once every weak encounter has appeared.
- Skipping weak encounters on a second pass. A longer list covers the usual case with no change to how the game reads the list.
- Turning the relic check off at the first Act 3 Treasure point, or at a fixed share of Act 3.
- Patching `PunchOff`, `SlipperyBridge`, or the monster seed.

To check in-game: the weak encounter count per act and how many combats an act really has, since the mixes are modeled.
