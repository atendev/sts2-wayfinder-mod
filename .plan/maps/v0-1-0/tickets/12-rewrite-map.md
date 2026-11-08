---
type: task
blocked_by: [07, 11]
undermined_by: []
assets: []
---

# Rewrite each act's map into a unified path

## Question

Add the Harmony prefix on `Hook.ModifyGeneratedMapLate` described in [the rewrite decision](./07-rewrite-integration.md). It skips `SavedActMap`, reads `EnableWayfinder` only for a new run's first map, seeds from `act_N_wayfinder`, keeps the start point at row 0, and writes one point per row in column 3 with an empty `startMapPoints`.
