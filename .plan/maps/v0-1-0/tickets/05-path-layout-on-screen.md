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

## Answer

The unified path is drawn as a switchback, and the switchback exists only on screen. In the data, each point has its own map row, rows are contiguous from 0, and every point is in center column 3.

Let N be the number of points between the Ancient and the boss. The layout works in `NMapScreen` pixels:

- The Ancient and the boss keep their stock positions, x -80 and x -200, which players see at bottom center and top center. Center for a normal point is column 3's stock x of -50, as on a vanilla map.
- The bottom rank holds 4 points, from x -50 to the right edge at x 400, at y 585.
- The middle ranks alternate direction. There are `round((N - 8) / 7)` of them, and each holds 6 to 8 points spread across the full width of 900 px, from x -500 to x 400.
- The top rank holds 4 points, from an edge to x -50, at y `740 - 2325 * scale`. `scale` is the game's own value: 1.0, or 0.9 when the act has a second boss.
- Ranks divide the height between those two y values evenly. That gives 271 px at 57 to 60 points and 241 px at 66, or 242 px and 215 px in an act with a second boss.
- Points keep the stock seeded jitter of ±21 px across and ±25 px vertically.
- With fewer than 26 points, the mod leaves the stock drawing in place. With every point in column 3 that is a single center column, the shape the game already draws for `GoldenPathActMap`. Ranks of 6 to 8 can't fit every count below 26.

One postfix on `NMapScreen.SetMap` moves the nodes, redraws every link through `CreatePath`, and reapplies the traveled color. `FightAllChoices` uses the same members after `SetMap`, and they exist in v0.107.1. The scroll window, the background and `Open` stay unpatched. `Open` scrolls by `row * _distY`, which tracks progress through the act, so the current point stays within one rank of where the screen opens.

The switchback gives the clearest read of what's ahead because the whole act fits in the stock scroll range, so the player sees up to several ranks of upcoming points at once. A tall column shows far fewer points per screen. The cost is that the eye has to follow the direction change at each turn. Spacing is 129 to 180 px across, against vanilla's 150. Mirroring the half rank at the top makes the last link a short step up to the boss.

The map's note that v0.1.0 changes ordering only is about what the act contains: the same points, in a new order. The `SetMap` postfix only draws that order, and the map already expected a drawing patch.

Excluded:

- A snake that shares map rows. It breaks `GetHistoryEntryFor` and makes `ActFloor` wrong, because `ActFloor` is `row + 1`.
- One tall column on a larger grid. It grows the scroll range about 4x, needs transpilers across `SetMap`, `UpdateScrollPosition`, `Open`, `ProcessControllerEvent` and `StartOfActAnim`, and probably needs longer background art.
- A zigzag from coords alone. It needs no patches, but at 60 points the stock formula rises only 39 px per step (2325 / 59), so points beside each turn sit 78 px apart.
- Fixed ranks of 7, with either a diagonal last link across up to half the map or leftover points packed 75 px apart.

To check in-game, because the decompile can't confirm them: node size, whether x -50 lines up visually with the Ancient and the boss, whether anything besides `SetMap` and `Open` reads `_distY`, and the real point counts, since 57 to 66 is an inferred figure.
