---
type: grilling
blocked_by: [01, 03]
undermined_by: []
assets: []
---

# Choose how the unified path is laid out on screen

## Question

The unified path is longer than a vanilla act has rows. How should it appear on the map screen: a snake back and forth across the existing grid, one tall column on a larger grid, or something else? Which option gives the player the clearest read of what's ahead, and what does each cost in patches to drawing and scrolling?

Research constraints: `RunState.GetHistoryEntryFor` indexes by row, so one point per row keeps the hover history correct, and a snake that shares rows does not. `GoldenPathActMap` is a one-point-per-row precedent. Vertical spacing and the scroll window are hard-coded, so a tall grid needs patches to stay readable.
