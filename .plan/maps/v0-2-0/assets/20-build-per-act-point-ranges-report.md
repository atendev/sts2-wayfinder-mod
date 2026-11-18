# Decision report: build per-act point ranges and the Path length setting

Branch `t3/per-act-point-ranges-path-length-2`, built from `1787494`. Council audit approved in round 2 of 5. No remote CI exists in this repo (no `.github/workflows`).

## Audit verdict

| Round | Claude seat | Gemini seat | GPT seat |
|---|---|---|---|
| 1 | 6, disapprove | 9, approve | 6, disapprove |
| 2 | 8, approve | 9, approve | 9, approve |

Round 1 raised one blocking problem per disapproving seat. Both were rebutted with evidence and the seats dropped them in round 2. Three minor problems were declined (see the last section).

## Decisions

### 1. Where each act's length and counts come from
- Question: how does `UnifiedActMap` get its mix once the vanilla counts are ignored?
- Options: clamp the vanilla counts into the ranges, or roll fresh from the run seed.
- Choice: `ActPlans.For(act - 1).Roll(rng)` rolls length, Elites, Unknowns, Rest sites, Shops and Treasures from the seed, and Monsters take the rest. A roll with no Monsters throws. The ticket "Decide per-act point ranges and act identities" had already excluded clamping because vanilla nearly always lands above the maximum.
- Made by: the earlier decision ticket. The main thread built it.

### 2. Underdocks
- Question: Act 1 includes Underdocks. Does it need its own branch?
- Choice: no. Underdocks is the alternate Act 1 and sits at act index 0. `RewriteMapPatch` passes `CurrentActIndex + 1`, so it gets the Build the deck plan. A doc comment on `ActPlans.BuildTheDeck` now says so.
- Made by: main thread, after the GPT seat raised it as blocking. The seat approved the rebuttal in round 2.

### 3. How the Act 3 pair rule fits the break order
- Question: where does the pair rule sit among the ordering rules that give way in a fixed order?
- Choice: it adds no step. A pair needs a rest site right before it and never grows to three, which belongs to the Elites apart rule (step 7). The 4-point clearance on either side belongs to the Elite gap rule (step 5). Both give way at those steps, so a pair never survives a rule that outranks it.
- Made by: the build in the earlier attempt, kept by the main thread.

### 4. Which existing rules become per act
- Choice: the Elite gap (5) takes an `EliteDistance` per act, 6 in Act 1. The opening ban (6) gains "no Elite in the first third" for Act 1. The pair rules above apply to Act 3. Everything else stays shared. `PlacementRules.Standard` reproduces v0.1 ordering.
- Made by: the build.

### 5. Act 2 Unknown runs
- Question: does Act 2 need its own run cap?
- Choice: no. The shared cap of 3 in a row (`SpreadPlacer.MaxRunOfOneType`) already existed, so runs of 2 to 3 were allowed. Act 2's change is its 8 to 12 Unknown range. A test checks that runs of 2 or more occur and none passes 3.
- Made by: main thread, after the Claude seat raised it as minor and then accepted the rebuttal.

### 6. Path length setting
- Choice: `DifficultyOptions.PathLength` (Acts or Full map), constructor default Full map, Acts in every preset including Off. Config section `MapPath`, read through `DifficultySettings.PathLengthFor`, snapshotted at run start through the existing JSON and written to the sidecar. Full map keeps `MixOf` and every generated point. Off uses Acts because the ticket says Acts in every preset.
- Made by: the build, per the ticket.

### 7. Encounter list size
- Choice: with Acts, `ExtendEncounterListPatch` sizes the normal list to the act's `MaxFightPoints` and keeps the weak draw doubled. Full map keeps the doubling.
- Made by: the build. The fallback to doubling when game state is missing is untested because it needs the game.

### 8. Curve numbers in docs
- Question: the Claude seat said the changelog and hover text claim a new curve with no code behind it.
- Choice: rebutted. `EnemyScalingCurve.cs:8-11` already holds 1.3, 1.5, 1.3 and 1.8 at base `a76bf19`. This diff only corrects the stale hover numbers and the changelog.
- Made by: main thread. The Claude seat confirmed with `git show` in round 2.

### 9. Scope
- Elite picks and random normal picks belong to the ticket "Build random encounter picks after each pool runs out". No code here touches them.

## Fixed after round 1
- Renamed a test that claimed more than it asserted, to `MostPathsHoldEveryRule`.
- Added the Underdocks doc comment.

## Minor problems declined
- Changelog "about 96 points a run" against a midpoint sum of 98. The wording comes from the decided ticket.
- "The widest Elite swing" for Act 2, when Act 2 and Act 3 both span 2. Also quoted from the decided ticket.
- `ScaleRoomCount` falls back silently to doubling. This is intended and cannot be tested without the game.

## Checks
- 118 tests pass in `tests/OrderingTests`. The mod builds. The BaseLib `min_version` line stays at 3.4.7.
- Untested: `ScaleRoomCount` in game. A playtest has to confirm the encounter list covers the longest act.
