---
type: task
blocked_by: [19]
undermined_by: []
assets: []
---

# Build per-act point ranges and the Path length setting

## Question

Build the per-act ranges from [Decide per-act point ranges](./19-per-act-point-ranges.md). `UnifiedActMap.MixOf` stops reading the vanilla map's counts and rolls each act's length and type counts from the run seed, with Monsters filling the rest. Act 1 includes Underdocks. The ordering rules gain the per-act placement flavor: no Elite in Act 1's first third and Act 1 Elites at least 6 apart, Act 2 Unknown runs of 2 to 3 allowed, Act 3 Elite pairs (never three in a row, rest site right before the pair, no other Elite within 4 points either side). How does the pair rule fit the break order, and which existing rules become per act? Add the Path length setting (Acts, Full map) to the difficulty settings with Acts in every preset, snapshot it at run start, and record it in the sidecar. Size the normal encounter list to the new act lengths instead of doubling it. Put the act identities in the mod description. Record the change in `docs/changelog.md`.

## Answer

Built on branch `t3/per-act-point-ranges-path-length-2`. `UnifiedActMap` rolls each act's length and counts from `ActPlans` and the run seed, Monsters fill the rest, and Underdocks takes the Act 1 plan at act index 0. The ordering rules take a per-act `PlacementRules`: Act 1 distance 6 and no Elite in the first third, Act 3 pairs with a rest site right before, never three, and 4 points clear on either side. The pair rule adds no break order step. It sits inside the Elites apart rule (step 7) and the Elite gap rule (step 5). The Path length setting (Acts or Full map) is Acts in every preset, is snapshotted at run start, and is recorded in the sidecar. The normal encounter list is sized to the act's longest path. The mod description, README, `CONTEXT.md` and `docs/changelog.md` are updated. The council approved in round 2 (claude 8, gemini 9, gpt 9). No CI exists. `ScaleRoomCount` is untested without the game. Report: [20-build-per-act-point-ranges-report.md](../assets/20-build-per-act-point-ranges-report.md).

Timing: wall 20m, active 19m, user 0s, wait 10s, blocked 0s | setup 5m, implement 7m, audit 5m (2 rounds) | tools 111 (9 subagent) | limits 0, compactions 0, handoffs 2 | ctx peak 159k | cost $4.10 sonnet-5-5 + $0.04 haiku-5-5
