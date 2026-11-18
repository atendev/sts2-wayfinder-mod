# Decision report: build random encounter picks

Ticket 21. Council: APPROVE in round 3 (claude 8, gemini 9, gpt 9). Two minor problems declined.

## Decisions

1. **Patch the getters or rewrite the lists?** Options: postfix `RoomSet.NextEliteEncounter` and `NextNormalEncounter`, or rewrite the lists after `ActModel.GenerateRooms`. Chosen: rewrite the lists. By: implementer. The lists are saved as ID lists by `RoomSet.ToSave`, and `RunManager.InitializeSavedRun` rebuilds them with `RoomSet.FromSave` and never calls `GenerateRooms`. A rewrite is therefore saved and reloaded with no extra state. A getter patch would need its own seeded pick keyed on the visit counter.
2. **Which rng?** Options: the generation `Rng` passed to `GenerateRooms`, a mod-owned `SeededOrderingRng`, or a fresh rng per pick. Chosen: the generation `Rng`, through a small `IOrderingRng` adapter. By: implementer. Draws happen once at run start, so a reload replays nothing. Only `NextInt(int)` is called, so the uint and ulong seed difference between builds does not matter.
3. **Elite list.** Each pool Elite once in a seeded shuffle, then random picks that skip the Elite just fought. By: implementer, changed after council round 2. The first draft kept vanilla's opening when it already held each Elite once. A council seat objected that vanilla's order follows a tag rule, so it is not a fresh random order. The branch was dropped. The opening is always a shuffle. The cost is that vanilla's tag-avoidance for Elites is gone. The vanilla list is only the fallback for an empty pool. The opening length is the pool size, which is 3 per act.
4. **Normal list.** Keep vanilla's list (weak encounters first, already doubled by the existing patch), then append random regular encounters. By: implementer. `ActModel.AllRegularEncounters` is `RoomType == Monster && !IsWeak` in the IL, so no weak encounter can be drawn. Unknown points that roll a fight call `PullNextEncounter(Monster)`, the same counter, so they need no code.
5. **Skip the previous normal fight (beyond the ticket).** Ticket says "each pick is a random regular encounter". I also skip the encounter just before it, so two identical fights never run back to back. By: implementer. Cheap and invisible otherwise.
6. **List lengths.** Elite 48, normal 96, as constants. By: implementer. A Full map act holds about 30 Elites and 35 normal fights at most, so the lists are not reached. Past the end vanilla's modulo wraps and replays the list. Declined adding a guard or log (see below).
7. **Gate.** `WayfinderConfig.EnableWayfinder`, like `ExtendEncounterListPatch`. All Wayfinder runs get it, Path length setting included, since ticket 20 owns that setting.
8. **Ordering with the existing patch.** `ExtendEncounterListPatch` is a transpiler inside `GenerateRooms`, so it runs before this postfix. The postfix sees the doubled list.

## Council

| Round | claude | gemini | gpt | Notes |
|---|---|---|---|---|
| 1 | 7 | 9 | 9 | claude blocking: weak exclusion unproven. |
| 2 | 7 | - | - | claude blocking: Elite opening kept vanilla's tag-driven order. Fixed by decision 3. |
| 3 | 8 | 9 | 9 | APPROVE. |

Round 1 blocking problem rebutted with the `get_AllRegularEncounters` IL. Round 2 blocking problem fixed. Fixed minor problems: a weak test assertion, changelog wording on when lists are drawn and the list sizes.

## Declined minor problems

- Harmony and field names (`_rooms`, `normalEncounters`, `eliteEncounters`, the `rng` parameter) only resolve at runtime. The names are verbatim in the IL dump and the mod builds. No in-game check ran. Left for the playtest.
- The 48 and 96 caps have no guard or log. The wrap is unreachable in a Full map act, and a log line would be noise.
- Pool size instead of the literal 3. Same rule for the shipped pools.
- Unknown fights sharing the normal counter has no test. It is vanilla behavior the mod does not touch.

## Open

- Not checked in game: the postfix patch itself and a save and reload of an act with extended lists. Unit tests cover the list building (102 pass).
