---
type: task
blocked_by: [19]
undermined_by: []
assets: [../../v0-1-0/assets/04-content-pools-on-long-path.md]
---

# Build random encounter picks after each pool runs out

## Question

Build the encounter picks from [Decide per-act point ranges](./19-per-act-point-ranges.md) for Wayfinder runs. Elites: the first 3 of an act are each pool Elite once in random order, then each pick is random from the pool, excluding the Elite just fought. Normal fights: once the act's normal list runs out, each pick is a random regular encounter, never a weak one. Vanilla indexes both lists with a visit counter modulo the list length (`RoomSet.NextEliteEncounter`, `NextNormalEncounter`), and the lists ride in the save. Should the mod patch the getters or rewrite the lists at generation, and which rng keeps the picks seeded and stable across a save and reload? Unknowns that roll a fight share the normal counter. Record the change in `docs/changelog.md`.

## Answer

Rewrite the lists at generation. A postfix on `ActModel.GenerateRooms` (`UnifiedPath/RandomEncounterPicksPatch.cs`, logic in `EncounterPicks.cs`) replaces each act's saved Elite and normal lists, using the generation `Rng`. The lists ride in the run save and a loaded run never calls `GenerateRooms`, so picks are seeded and stable across a save and reload with no extra state. A getter patch would have needed its own seeded pick keyed on the visit counter.

- Elites: each pool Elite once in a seeded shuffle, then random picks that skip the Elite just fought. List length 48.
- Normal fights: vanilla's list unchanged (weak first), then random regular encounters that skip the previous one, length 96. `AllRegularEncounters` is `Monster && !IsWeak`, so no weak encounter returns. Unknowns that roll a fight use the same counter with no extra code.
- Gated on Enable Wayfinder, like the existing encounter patch. Past the list the vanilla modulo wraps, which a Full map act does not reach.

Excluded: a getter patch, a mod-owned rng, keeping vanilla's tag-driven Elite opening. Not checked in game: the patch itself and a reload of an act with extended lists. Changelog and README updated. Report: `../assets/21-build-random-encounter-picks-report.md`.

Timing: wall 7m, active 7m, user 5s, wait 0s, blocked 0s | implement 2m, audit 2m (3 rounds), fix 1m, gate 34s | tools 54 (6 subagent) | limits 0, compactions 0, handoffs 1 | ctx peak 145k | cost $0.03 haiku-5-5 + $1.78 sonnet-5-5
