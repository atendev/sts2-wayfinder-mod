# Content pools on a longer act

Ticket: 04-content-pools-on-long-path. Game version v0.107.1.

Sources: `references/decompiled/sts2-il.txt` (full IL) and `sts2.txt` (call lists). Everything below was read from IL unless it carries the tag **[inferred]**. Method names are given so each claim can be re-found with grep.

## Short answer

Visiting every point never throws on its own. The normal-encounter, elite-encounter and event lists are built once per act and indexed with a visit counter taken modulo the list length, so they wrap and repeat. Events additionally skip already-seen events until all are used, then repeat with a logged warning. Relics fall back to a fixed placeholder relic when the pool is empty. Unknown-point odds are a per-act running tally with no dependence on how many points there are. Total floor count affects nothing in these pools.

The one real consequence for a long path: the normal list is only 13 to 15 entries long (12 to 14 in multiplayer) and its first 2 or 3 entries are the weak encounters. A unified path with more normal combats than that will start reusing the list from the top, so weak encounters return late in the act.

## Where the lists are built

`RunManager.GenerateRooms()` runs once per run, at run start, for every act. It shuffles the shared ancients with the `UpFront` rng, hands each act after the first a random subset, then calls `ActModel.GenerateRooms(rng, unlockState, isMultiplayer)` per act. At ascension 10 it also picks a second boss for the last act. Nothing here depends on the map, so the lists are fixed before any map exists. **[inferred]** this means replacing the map does not change what the lists contain, only how far down them the player gets.

`ActModel.GenerateRooms` fills a `RoomSet` (`_rooms`) with these lists:

| List | Length | How it is filled |
|---|---|---|
| `events` | act events plus all shared events, minus events whose epoch is unrevealed | shuffled with `UnstableShuffle`, then appended |
| `normalEncounters` | `GetNumberOfRooms(isMultiplayer)` | first `NumberOfWeakEncounters` from the weak grab bag, then the rest from the regular grab bag |
| `eliteEncounters` | 15 (literal `ldc.i4.s 15`) | from the elite grab bag |
| boss | 1, plus a second boss at ascension 10 on the last act | `Rng.NextItem` over `AllBossEncounters` |
| ancient | 1 | `Rng.NextItem` over unlocked ancients plus the shared subset |

Per act constants (read from each `Acts.*` type):

| Act | `BaseNumberOfRooms` | `NumberOfWeakEncounters` |
|---|---|---|
| Overgrowth | 15 | 3 |
| Underdocks | 15 | 3 |
| Hive | 14 | 2 |
| Glory | 13 | 2 |

Multiplayer subtracts 1 from the room count (`GetNumberOfRooms`). The base class default for weak encounters is 3. `GetNumberOfFloors` is `rooms + 2`; its only caller in the dump is a main-menu UI class (`NContinueRunInfo`), so it is display only.

The normal list therefore holds weak encounters at indexes 0 to 1 (or 0 to 2) and regular encounters from there to the end. The number 15 for elites also equals the `maxElites` constant in `StandardActMap`, so the elite list was sized to the largest elite count the vanilla map allows (8 elites at ascension 1 or higher, 5 otherwise, by `MapPointTypeCounts`).

### Grab bag behavior

Each list is built by a loop that, whenever its `GrabBag<EncounterModel>` is empty, refills it from the full pool, then calls `AddWithoutRepeatingTags`. That helper calls `GrabAndRemove` with a predicate that rejects encounters sharing a tag with ones already added, and if nothing passes it retries with no predicate. `GrabAndRemove` removes the pick, so inside one pass an encounter is never drawn twice. If the pool is smaller than the list length, the bag refills and duplicates appear inside the list itself. How many encounters each act's pool holds was not counted. **[inferred]** the tag predicate looks at everything already in the list, not just the previous entry; the closure body `<AddWithoutRepeatingTags>b__0` was not read.

If a pool were empty, `GrabAndRemove` returns null (`GrabIndex` returns -1), `AddWithoutRepeatingTags` adds nothing, and the list would stay short. That is not reachable with shipped content. **[inferred]**

## Choosing the next entry

`ActModel.PullNextEncounter(roomType)` switches on `roomType - 1`: Monster returns `RoomSet.NextNormalEncounter`, Elite returns `NextEliteEncounter`, Boss returns `NextBossEncounter`. Any other room type throws `ArgumentOutOfRangeException`. `PullNextEvent` calls `RoomSet.EnsureNextEventIsValid`, then `Hook.ModifyNextEvent`, then records the event in `RunState.VisitedEventIds`.

`RoomSet` getters (all read from IL):

- `NextNormalEncounter` is `normalEncounters[normalEncountersVisited % normalEncounters.Count]`.
- `NextEliteEncounter` is `eliteEncounters[eliteEncountersVisited % eliteEncounters.Count]`.
- `NextEvent` is `events[eventsVisited % events.Count]`.
- `NextBossEncounter` returns the second boss if `bossEncountersVisited != 0` and a second boss exists, else the boss.

The pull does not advance the counter. The counter moves in `RoomSet.MarkVisited`, called through `ActModel.MarkRoomVisited` from `RunManager.EnterRoomInternal`, and only when `RunState.CurrentRoomCount == 1`. `CurrentRoomCount` is the number of rooms entered at the current point (an event that spawns a fight is a second room at the same point), so each point advances its counter once and a fight spawned from inside an event does not. **[inferred]** from the guard; I did not read what increments `CurrentRoomCount`. The `MarkVisited` switch also increments `eventsVisited` for Event and `bossEncountersVisited` for Boss. Treasure, shop and rest site have no counter.

## Per pool: wrap, repeat or throw

| Pool | Exhaustion behavior | Can it throw |
|---|---|---|
| Normal encounters | Wraps by modulo and repeats from index 0. Weak encounters recur on the second pass. | Only if the list is empty (division by zero). Not reachable in normal play. **[inferred]** |
| Elite encounters | Wraps by modulo after 15 elites. | Same as above. |
| Events | Skips events that are disallowed or already in `VisitedEventIds` by advancing `eventsVisited`, up to `events.Count` tries. If every event is used, it logs "All unique events exhausted, allowing repetition" (Warn) and then serves `NextEvent` anyway, so it repeats. After that it wraps by modulo. | Only if the list is empty: `EnsureNextEventIsValid` returns early and `NextEvent` divides by zero. |
| Boss | Returns the boss, or the second boss after the first boss is marked visited. | `RoomSet.Boss` throws `InvalidOperationException` if never set, which only happens if `GenerateRooms` was skipped. Ancient has the same guard. |
| Relics (treasure, shop, rewards) | `RelicGrabBag.GetAvailableDeque` drops disallowed relics, refreshes the rarity deque if refresh is allowed, then steps down through lower rarities, then a multiplayer fallback deque. If all fail it returns null and `RelicFactory.PullNextRelicFromFront` substitutes `RelicFactory.FallbackRelic` (a `ModelDb.Relic<T>` placeholder; which relic was not resolved). | No. |

`EnsureNextEventIsValid` advances `eventsVisited` while searching, so on a full wrap the counter has moved by `events.Count` and the same event is served again. **[inferred]** from the loop; it follows from the IL but I did not simulate it.

`ValidateRoomsAfterLoad` only repairs deprecated boss entries after a save load. It does not touch the lists.

## Weak to normal switch

There is no runtime switch. The weak/normal split is baked into list order at generation: indexes `0` to `NumberOfWeakEncounters - 1` are weak, the rest are regular. The visit counter simply walks the list, so the first 2 or 3 normal combats of an act are weak, then regular ones. On the unified path the second pass through the list, if the path has more normal combats than the list length, would serve weak encounters again after the regular ones. **[inferred]** The counter is shared with unknown points that roll Monster, since `RollRoomTypeFor` returns Monster and `CreateRoom` then calls `PullNextEncounter(Monster)` followed by the same `MarkVisited`. So an unknown that becomes a fight spends a weak slot if it comes early.

Counter is per act: `ActModel` owns its own `_rooms`, and `RunManager.SetActInternal` does not reset it because each act has its own set.

## Unknown-point odds

`RunManager.RollRoomTypeFor(pointType, blacklist)`: for `Unknown` points it calls `RunState.Odds.UnknownMapPoint.Roll(blacklist, runState)`; other point types map straight to a room type (Shop to Shop, Treasure to Treasure, Rest to RestSite, Monster to Monster, Elite to Elite, Boss to Boss, Ancient to Event). `TryGetRoomTypeForTutorial` is checked first.

`UnknownMapPointOdds` state: two dictionaries, `_baseOdds` and `_nonEventOdds`, both starting at Monster 0.1, Elite -1, Treasure 0.02, Shop 0.03. Event odds are derived: `max(0, 1 - sum of positive entries)`. The float constants `baseMonsterOdds` (0.1), `baseEliteOdds` (-1), `baseTreasureOdds` (0.02), `baseShopOdds` (0.03) match.

`Roll`:

1. If `UnlockState.NumberOfRuns == 0` (first run ever), count prior Unknown points in `MapPointHistory`: fewer than 2 gives Event, exactly 2 gives Monster. After that normal rolling starts.
2. Build the allowed set: the four tracked types plus Event, minus the blacklist, then `Hook.ModifyUnknownMapPointRoomTypes`.
3. Draw one `NextFloat(1)` from the odds rng and walk `_nonEventOdds` in insertion order (Monster, Elite, Treasure, Shop), skipping negatives and disallowed types, accumulating. The first type where the draw is at or below the running total wins. If none wins, the result is Event.
4. Update: the winner's entry resets to its base value. Every other allowed type gains its base value (passed through `Hook.ModifyOddsIncreaseForUnrolledRoomType`, which by default returns the amount unchanged).

Consequences for a long act:

- Elite can never roll from an unknown point in vanilla. Its entry starts at -1 and each unrolled step adds -1 (more negative), and negatives are skipped. The `DeadlyEvents` modifier is the one place that sets elite odds to 0.1 and base elite to 0.1 (read from its `AfterRunStarted`/`AfterRunLoaded`).
- Probabilities rise with each unknown that did not roll that type: Monster gains 0.1 per miss, Treasure 0.02, Shop 0.03. Nothing caps them. After 9 misses Monster is at 1.0 and Event becomes impossible until it hits. So over many unknown points the draws cluster toward Monster, with Treasure and Shop appearing occasionally and rarely more than a couple of times. This is a state machine over unknown points only; it does not look at the total number of points. The number of unknowns on a unified path matters only because each one is another roll.
- Odds reset to base at the start of each act: `SetActInternal` calls `UnknownMapPointOdds.ResetToBase`.
- The `DeadlyEvents` modifier doubles the treasure increase (`ModifyOddsIncreaseForUnrolledRoomType` multiplies it by 2).
- `BuildRoomTypeBlacklist(previousMapPointEntry, nextMapPoints)` adds Shop (room type 5) to the blacklist when the previous point already held a shop, or when every child of the current map point is a shop. On a strict line the children are the next point, so the "all children shops" test reduces to "next point is a shop". The mod must give `CurrentMapPoint.Children` a sensible value or this check sees an empty set (the code treats an empty or null child set as not blacklisted). **[inferred]** from the `HasRoomOfType` and `All` calls; the predicate body `<BuildRoomTypeBlacklist>b__171_0` was not read.

Unknown count per vanilla map comes from `GetMapPointTypes`: Gaussian around 12 (clamped 10 to 14), with rest sites around 7 (Overgrowth, Underdocks), 6 (Hive), 5 to 7 (Glory, uniform). Elites 5, or 8 at ascension 1 or higher; shops 3. Treasure and monster counts are not in `MapPointTypeCounts`; they fall out of `StandardActMap.AssignPointTypes`, which is outside this ticket.

## Treasure and shop

- Treasure: `TreasureRoom(actIndex)` rewards come from `RelicFactory` (rarity rolled from the player's `Rewards` rng at 50% / 83% / remainder thresholds visible in `RollRarity`), pulled from the per-player `RelicGrabBag`, then removed from the shared bag. Empty pool gives the fallback relic, never an exception. Number of treasure points on the path does not change anything else.
- Shop: `MerchantInventory` is built per `MerchantRoom` with `PlayerRngSet.Shops`; `PopulateRelicEntries` rolls rarities and pulls relics the same way, so it drains the same relic bag and can reach the fallback. Card and potion stock generation was not read. **[inferred]** they sample from pools without consuming a depletable bag, so a third shop is not different in kind from the first.
- Neither room holds a visit counter in `RoomSet`.

## Does total floor count matter

No pool reads it. `GetNumberOfFloors` (`rooms + 2`) is only called from a UI class. `GetNumberOfRooms` is read by `StandardActMap` (map row count) and by `ActModel.GenerateRooms` (normal list length), so it sizes the normal list but is a fixed per-act constant, not a function of how many points the player visits. `RunState.TotalFloor` exists but no pool code in this investigation called it. **[inferred]** from the absence of callers in the searched lists.

## What this means for the mod

1. The mod does not need to extend any pool to avoid crashes. Modulo indexing already handles any number of visits.
2. The visible effect is repetition: with 13 to 15 normal slots, a unified path of far more than that many normal-or-monster-unknown combats replays the list, including weak encounters. If that is undesirable, the mod must post-process `RoomSet.normalEncounters` (private field `normalEncounters` on `RoomSet`) or patch `NextNormalEncounter`. Event repetition is already handled by game code and only produces a warning.
3. Elite count: the list holds 15, so the unified path would need more than 15 elite points before repeating. Vanilla maxes at 8, so this is not a concern.
4. Unknown points will drift toward Monster on a long run of unknowns. Whether that matters is a design choice for the ordering rules.
5. The mod must keep `RunState.CurrentMapPoint` and its `Children` meaningful, because the shop blacklist reads them and `MarkVisited` is gated on `CurrentRoomCount`.

## Not covered or unresolved

- Pool sizes (how many weak, regular, elite encounters or events exist per act) were not counted.
- Which relic `FallbackRelic` is.
- Closure bodies for the tag predicate and the shop blacklist predicate.
- Card and potion stock generation in shops.

## Correction

The section on floor count above covers `GetNumberOfFloors`. `RunState.TotalFloor` is a separate value that feeds `EncounterModel.GenerateMonstersWithSlots`, `PunchOff.IsAllowed`, `SlipperyBridge.IsAllowed`, and `RelicModel.IsBeforeAct3TreasureChest`, so floor count does affect gameplay on a longer path.
