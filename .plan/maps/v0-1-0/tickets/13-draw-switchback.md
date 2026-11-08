---
type: task
blocked_by: [05, 12]
undermined_by: []
assets: []
---

# Draw the unified path as a switchback

## Question

Add the `SetMap` postfix from [the layout decision](./05-path-layout-on-screen.md). It recognizes a unified path by shape, positions ranks in `NMapScreen` pixels, and leaves the stock drawing in place below 26 points.

## Answer

Built as `UnifiedPath/DrawSwitchbackPatch.cs`, a postfix on `NMapScreen.SetMap`, and `Layout/SwitchbackLayout.cs`, which holds the rank math with no game references and is linked into the test project. The patch follows [the layout decision](./05-path-layout-on-screen.md). It uses `UnifiedActMap.IsLine` to recognize the shape. Paths under 26 points return early, so a Wayfinder `GoldenPathActMap` keeps the stock column.

Choices the ticket left open:

- Lower middle ranks take the leftover points, one extra each.
- Jitter is recovered as the node's position minus its stock position, so the seeded values stay the same.
- Every entry in `_paths` is redrawn, including the start, boss, and second boss links.
- Private members are reached through Harmony field injection and `AccessTools.MethodDelegate`, because the project keeps `Publicize` off.

The v0.107.1 IL settles one check [the layout decision](./05-path-layout-on-screen.md) left open: `ProcessControllerEvent` is the only other reader of `_distY`, and it scrolls by `row * _distY`, the same as `Open`, so it needs no patch.

Verified: the mod builds against `sts2.dll`, all 21 tests pass, and a throwaway harness on the real `sts2.dll` resolved both private methods and matched every patch parameter to the game's field names and types. Not verified: the patch applying or drawing in-game. The harness could not apply it, because Harmony rejects the .NET 10 runtime installed here. [The playthrough](./18-playthrough.md) covers that, along with node size, whether x -50 lines up with the Ancient and the boss, and the real point counts.

Excluded:

- Generating new jitter, which would drop the stock seeded values.
- Redrawing only the links that moved, which saves little and needs more code.
