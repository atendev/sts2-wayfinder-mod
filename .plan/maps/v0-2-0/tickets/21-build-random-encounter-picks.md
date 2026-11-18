---
type: task
blocked_by: [19]
undermined_by: []
assets: [../../v0-1-0/assets/04-content-pools-on-long-path.md]
---

# Build random encounter picks after each pool runs out

## Question

Build the encounter picks from [Decide per-act point ranges](./19-per-act-point-ranges.md) for Wayfinder runs. Elites: the first 3 of an act are each pool Elite once in random order, then each pick is random from the pool, excluding the Elite just fought. Normal fights: once the act's normal list runs out, each pick is a random regular encounter, never a weak one. Vanilla indexes both lists with a visit counter modulo the list length (`RoomSet.NextEliteEncounter`, `NextNormalEncounter`), and the lists ride in the save. Should the mod patch the getters or rewrite the lists at generation, and which rng keeps the picks seeded and stable across a save and reload? Unknowns that roll a fight share the normal counter. Record the change in `docs/changelog.md`.
