---
type: grilling
blocked_by: []
undermined_by: []
assets: []
---

# Decide per-act point ranges and act identities

## Question

Raising enemy HP kept chasing player power: run 3 ended with 63 relics and a 71-card deck at 360% HP. The unified path holds every point the vanilla map generated, on average 58 / 48 / 51 points between start and boss against 15 / 14 / 13 walked in vanilla, so a won run is 157 points and about 122 minutes against vanilla's 42 points and 49 minutes. Elites, Treasures, and Unknowns come about 3.5 times as often as vanilla. Should the path be shorter, and how should each act's mix be set?

Measured from 8 vanilla and 7 Wayfinder wins in the local run history. Each act's Elite pool holds exactly 3 encounters. A simulation of the vanilla generator shows fewer paths (4 instead of 7) would shrink a map to about 40 / 37 / 34, but the vanilla type targets (5 Elites, 10 to 14 Unknowns, 3 Shops) stay fixed, so the cut would come almost entirely out of Monsters.

## Answer

Each act gets its own count range per point type and its own length. The counts are rolled from the run seed; the vanilla map supplies only the start point and the boss, and its mix is ignored. Monsters fill whatever the length leaves.

| | Act 1: Build the deck | Act 2: The wilds | Act 3: The gauntlet |
|---|---|---|---|
| Length | 34 to 38 | 32 to 36 | 26 to 30 |
| Elites | 3 to 4 | 3 to 5 | 4 to 6 |
| Unknowns | 3 to 5 | 8 to 12 | 2 to 4 |
| Rest sites | 5 to 6 | 4 to 5 | 3 to 4 |
| Shops | 2 | 3 | 1 |
| Treasures | 2 | 1 to 2 | 1 |
| Monsters | the rest | the rest | the rest |

About 96 points a run. The Elite minimum is 3 in every act, the size of each act's Elite pool, so every Elite is fought at least once.

Act identities, used for tuning and in the mod description, never shown in game:

- **Act 1, Build the deck.** Fights and card rewards drive growth, Unknowns are rare, Elites come later and spread out, rest is generous, two Shops let the deck be trimmed. No Elite in the first third of the path, Elites at least 6 points apart.
- **Act 2, The wilds.** The most Unknowns and the fewest fixed fights, Unknowns may come in runs of 2 to 3, the most Shops, the widest Elite swing. The run's build turns here.
- **Act 3, The gauntlet.** Shortest and most Elite-dense, the fewest rests, one Shop and one Treasure. Two Elites may sit side by side, never three. A pair has a rest site right before it and no other Elite within 4 points on either side. A rest site always comes before the boss.

Encounter picks:

- Elites: the first 3 of an act are each pool Elite once in random order. After that every Elite is a random pick from the pool, never the one just fought.
- Normal fights: once the act's normal list runs out, each fight is a random regular encounter. Weak encounters never return late in the act.

A Path length setting keeps the old behavior: **Acts** (these ranges, the default in every preset) or **Full map** (every generated point, as in v0.1).

The enemy scaling curve drops with the shorter path, since less player power needs less enemy scaling: path HP 130% / 150% and path damage 130% / 180% at the end of Act 2 / Act 3. Bosses stay as they are.

Excluded: shrinking the vanilla generator (fewer paths), because it cuts Monsters and keeps Elites and Unknowns. Clamping the vanilla counts into the ranges, because vanilla nearly always lands above the maximum. Cutting points only when they break an ordering rule, because the length would swing with the rules; the Act 3 Elite pair rule is the one rule change. Showing act names in game.

This reverses the earlier settled note that every generated point stays on the path.
