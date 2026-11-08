---
type: task
blocked_by: [09, 12]
undermined_by: []
assets: []
---

# Turn the Act 3 chest relic check off when Act 3 starts

## Question

Patch `RelicModel.IsBeforeAct3TreasureChest` so a Wayfinder run returns `CurrentActIndex < 2`, as [the content decision](./09-content-built-for-shorter-acts.md) describes. Other runs keep the vanilla `TotalFloor` check.

## Answer

`Act3RelicCheckPatch` is a Harmony postfix on `RelicModel.IsBeforeAct3TreasureChest`. When `UnifiedActMap.IsLine(runState.Map)` is true, it sets the result to `CurrentActIndex < 2`, which replaces both the single-player and multiplayer thresholds. Every other run keeps the vanilla result.

Two timing cases need no extra code:

- Before any map exists, `RunState.Map` is `NullActMap`, whose empty grid fails `IsLine`, so the vanilla result stands. On a new run that is floor 0 of Act 1, where both checks return true.
- Between acts the previous map stays set until the next one is generated. The game sets `CurrentActIndex` before it calls `GenerateMap`, so the check returns false as soon as Act 3 begins.

Verified by build and the 21 ordering tests, with the method's signature and the timing read from the IL dump. The patch has not been applied in the running game. The method is 31 bytes of IL, so [the playthrough](./18-playthrough.md) has to confirm the JIT does not inline it into the relic `IsAllowed` overrides.

Excluded:

- A prefix that skips the vanilla method, since vanilla has no side effects to avoid.
- Reading `EnableWayfinder` here, since [the content decision](./09-content-built-for-shorter-acts.md) uses the map's shape once a map exists.
