---
type: research
blocked_by: []
undermined_by: []
assets: [03-map-screen-and-travel.md]
---

# Trace how the map screen draws and travels

## Question

How does `NMapScreen` turn grid coords into screen positions, and what limits its scrolling? How are paths between points drawn, and can it draw same-row links or rows beyond the vanilla count? Can `Grid` be larger than a vanilla act's, and what breaks if it is? How does `MapTravel.GetTravelablePointsFrom` decide legal moves? Do the private members `FightAllChoices` relies on (`startMapPoints`, `_mapPointDictionary`, `_paths`, `GetLineEndpoint`, `CreatePath`) exist in game v0.107.1, given that mod declares 0.109.0? Does BaseLib offer anything relevant?

## Answer

Findings are in [the map screen research](../assets/03-map-screen-and-travel.md).

Vertical spacing is `2325 / (rows - 1)` and the scroll window is hard-coded to Y between -600 and 1800. A taller grid therefore packs points closer together unless the mod patches those values. `CreatePath` draws straight dotted lines, so same-row and long links both draw. `Grid` can be any size and saves round-trip it, with the boss at row `GetRowCount()`.

`GetTravelablePointsFrom` returns the point's `Children`, so a line of single children needs no travel patch. All five private members `FightAllChoices` uses exist in v0.107.1. BaseLib 3.4.7 offers act-level hooks such as `CustomCreateMap` and nothing for the map screen.
