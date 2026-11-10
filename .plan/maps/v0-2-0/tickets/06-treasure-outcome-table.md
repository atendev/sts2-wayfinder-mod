---
type: grilling
blocked_by: [03]
undermined_by: []
assets: []
---

# Decide the treasure outcome table

## Question

In Power curve, a treasure chest rolls an outcome instead of always giving a relic. What outcomes are in the table, such as a relic with a small amount of gold, a large amount of gold with no relic, or another combination? What gold amounts and odds does each outcome have, does it change by act, and which parts are tuning values? How does the table interact with relics that change chests, and with the Act 3 chest relic check from v0.1.0?

## Answer

The table has two outcomes. A **relic chest** is vanilla: a relic plus 42 to 52 gold. A **gold chest** has no relic and gives twice vanilla's gold roll, 84 to 104, with the gold tuning value applied on top. It shows the existing empty-chest VFX.

The relic chest chance rises within each act. The first chest of an act has a 20% relic chance. Each gold chest adds 20%, and a relic chest resets the chance to 20%. If an act reaches its last treasure point with no relic chest yet, that chest is a relic chest. The miss count and the chance reset at each act's start. A typical 4-chest act goes 20%, 40%, 60%, then a forced relic, and averages about 1.4 chest relics. That is about 4 per run, against vanilla's 3 and v0.1's 11 to 12. Expected relics per act for other starting chances and steps, assuming a reset after each relic chest:

| Start / step | 3 chests | 4 chests | 5 chests |
| --- | --- | --- | --- |
| 25% / +25% | 1.27 | 1.62 | 2.06 |
| 20% / +20% | 1.18 | 1.43 | 1.79 |
| 15% / +15% | 1.10 | 1.27 | 1.51 |

Without the reset, a 4-chest act averages 2.5 chest relics, so the reset is required.

Tuning values: **Chest relic chance** (20%), **Chest relic chance step** (+20%), and **Gold chest multiplier** (2x). Off and Bosses only set the chance to 100%, which is vanilla. The forced last chest is always on and isn't a tuning value, because it only matters when the chance is under 100%.

Silver Crucible's emptied chest stays vanilla: no relic, no gold, no roll. It doesn't count as a miss. If it is the act's last chest, that act can end with no chest relic. The Act 3 chest relic check from v0.1.0 doesn't change. It only decides when White Star and Lasting Candy can be offered, and it never reads chest contents.

Excluded: a potion outcome (chests can't show one without a new reward screen), a fully empty outcome, odds that differ by act, a per-run guarantee, and a fixed every-Nth-chest pattern.

Both relic chance and miss count can be rebuilt from the map point history and the fixed unified path, so this adds no save data. Build: [Build the treasure outcome table](./10-build-treasure-outcome-table.md).
