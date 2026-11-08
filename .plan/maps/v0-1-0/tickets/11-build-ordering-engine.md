---
type: task
blocked_by: [06, 08, 10]
undermined_by: []
assets: []
---

# Build the ordering engine

## Question

Build the code that turns an act's mix of points and a seeded RNG into one ordered path. It implements the hard rules, preferences, and break order from [the ordering decision](./06-ordering-rules.md), plus the cap of 4 Monsters and Elites in a row from [the preview](./08-preview-sample-paths.md), which gives way before any other preference.
