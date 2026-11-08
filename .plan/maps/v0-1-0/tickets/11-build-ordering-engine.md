---
type: task
blocked_by: [06, 08, 10]
undermined_by: []
assets: []
---

# Build the ordering engine

## Question

Build the code that turns an act's mix of points and a seeded RNG into one ordered path. It implements the hard rules, preferences, and break order from [the ordering decision](./06-ordering-rules.md), plus the cap of 4 Monsters and Elites in a row from [the preview](./08-preview-sample-paths.md), which gives way before any other preference.

## Answer

The engine is built in `sts2_wayfinder_modCode/Ordering/`, uses only the base class library, and is not connected to the game yet. `PathOrderer.Order` takes a `PointMix` and an `IOrderingRng` and returns the path, the break-order step it reached, and the rules it broke. `tests/OrderingTests` compiles the same files through linked items and runs all seven checks from [the test decision](./10-test-ordering-engine.md). All 9 test cases pass on the .NET 10 runtime. Five deliberate faults in the engine each made tests fail.

The build differs from the model or the decision text in these ways:

- Elite gap 1 and gap 0 are one step. In the model both only stop Elites touching, so the steps now match the numbers 1 to 7 in [the ordering decision](./06-ordering-rules.md).
- Jitter and weighted picks use integers only, because `Math.Pow` can differ between platforms and co-op later needs identical paths.
- When the fight cap can't be met, the uncapped attempt continues the RNG stream. It stays deterministic. [The preview](./08-preview-sample-paths.md) says "from the same seed", but `IOrderingRng` has no reset.
- `ElitesApart` is restored first, as the ordering decision requires. The model never restored it.

Kept from the model: mixes under 8 points, or without a Monster or a Rest site, skip the spread and report preferences as dropped. The search keeps its budget of 50,000 nodes, which the test suite never reached, so running out of budget is untested.

Excluded:

- Reporting a dropped fight cap in the result. The test decision leaves that to the validator.
- A signal for an exhausted search budget, because no test mix reaches it.
