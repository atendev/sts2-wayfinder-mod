---
type: grilling
blocked_by: [01, 02, 04]
undermined_by: []
assets: []
---

# Decide how to handle content built for shorter acts

## Question

An act's normal encounter list has 13 to 15 entries and wraps when exhausted, so a path with more combats replays them, and the weak opening encounters return mid-act. Is that acceptable for v0.1.0, or should the mod change how the list is consumed? Floor checks also shift on a longer path: `EncounterModel.GenerateMonstersWithSlots`, the `PunchOff` and `SlipperyBridge` conditions, and `RelicModel.IsBeforeAct3TreasureChest` all read `RunState.TotalFloor`. Should the mod leave those alone? The map's scope limits v0.1.0 to ordering changes, so any change here needs to be justified against that limit.
