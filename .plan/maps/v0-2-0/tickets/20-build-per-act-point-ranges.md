---
type: task
blocked_by: [19]
undermined_by: []
assets: []
---

# Build per-act point ranges and the Path length setting

## Question

Build the per-act ranges from [Decide per-act point ranges](./19-per-act-point-ranges.md). `UnifiedActMap.MixOf` stops reading the vanilla map's counts and rolls each act's length and type counts from the run seed, with Monsters filling the rest. Act 1 includes Underdocks. The ordering rules gain the per-act placement flavor: no Elite in Act 1's first third and Act 1 Elites at least 6 apart, Act 2 Unknown runs of 2 to 3 allowed, Act 3 Elite pairs (never three in a row, rest site right before the pair, no other Elite within 4 points either side). How does the pair rule fit the break order, and which existing rules become per act? Add the Path length setting (Acts, Full map) to the difficulty settings with Acts in every preset, snapshot it at run start, and record it in the sidecar. Size the normal encounter list to the new act lengths instead of doubling it. Put the act identities in the mod description. Record the change in `docs/changelog.md`.
