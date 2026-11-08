---
type: research
blocked_by: []
undermined_by: []
assets: [04-content-pools-on-long-path.md]
---

# Find how content pools behave on a longer act

## Question

Vanilla players visit one route, and the unified path visits every point. What happens to encounters, events, and elites when an act has far more visits than usual? Look at `GenerateRooms`: how many of each encounter does it prepare, does it wrap or repeat when exhausted, and can it run out or throw? How does the switch from weak to normal encounters work? How do unknown-point odds (`UnknownMapPointOdds`), treasure, and shop contents behave across many visits? Does total floor count affect anything?

## Answer

Findings are in [the content pools research](../assets/04-content-pools-on-long-path.md).

Nothing runs out or throws with real content. Normal encounters (13 to 15 entries, the first 2 or 3 weak), elites (15), and events all wrap with `visited % Count`. The weak-to-normal switch is only list order, so weak encounters come back on a second pass. Events skip seen ones until all are used, then repeat. Relics fall back to a placeholder when the pool is empty.

Unknown odds are a running tally that resets each act. The shop blacklist reads `CurrentMapPoint.Children`, so the mod must keep that set correct.

Correction to the research file: it covers `GetNumberOfFloors`, which only a UI class reads. `RunState.TotalFloor` does affect gameplay, as recorded in [the map lifecycle ticket](./02-map-lifecycle-and-dependents.md). A longer path shifts those floor checks.
