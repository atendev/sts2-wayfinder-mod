---
type: task
blocked_by: [09, 12]
undermined_by: []
assets: []
---

# Turn the Act 3 chest relic check off when Act 3 starts

## Question

Patch `RelicModel.IsBeforeAct3TreasureChest` so a Wayfinder run returns `CurrentActIndex < 2`, as [the content decision](./09-content-built-for-shorter-acts.md) describes. Other runs keep the vanilla `TotalFloor` check.
