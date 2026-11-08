---
type: grilling
blocked_by: [06]
undermined_by: []
assets: []
---

# Decide how the ordering engine gets tested without the game

## Question

The repo has no test project, and the mod references the game assembly. How do the ordering rules and break order from [the ordering decision](./06-ordering-rules.md) get tested without the game running? Decide where the engine lives so it has no game dependency, which test runner to use, and which checks are required, such as determinism from a seed and every hard rule holding on batches of real mixes.
