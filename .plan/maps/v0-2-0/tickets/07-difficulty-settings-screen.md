---
type: prototype
blocked_by: [04]
undermined_by: []
assets: [07-difficulty-settings-screen.html]
---

# Decide the difficulty settings screen

## Question

How do the difficulty settings appear in the mod config screen? Where does the preset choice sit relative to EnableWayfinder, how are tuning values grouped (enemy scaling, card rewards, treasure, Elite relic rarity, gold), and what does the player see when a hand edit turns the preset into Custom? What labels and hover text explain each value, and that settings take effect on a new run? Build a rough mock within what BaseLib's config supports to react to.

## Answer

Layout A of [the mock](../assets/07-difficulty-settings-screen.html): the preset dropdown sits right under Enable Wayfinder, followed by one collapsible section per group, all open and always visible. The settings are toggles and named choices, not number sliders. Hover tips carry the explanations.

- **Difficulty preset** (Off, Bosses only, Power curve, Custom). Its hover tip says it takes effect on a new run. Moving any option to something the active preset doesn't set flips the dropdown to Custom and shows a banner: "Custom: options no longer match a preset. Pick a preset to reset them." Picking a preset sets every option below.
- **Enemy scaling**: Scale bosses, Scale fight HP along the path, Scale fight damage along the path. Each tip gives the rough numbers.
- **Card rewards**: Alternate card rewards (toggle) and Card choices (3 to 5, the only slider).
- **Treasure chests**: Use treasure chest chances (toggle), then Chest odds (Generous, Balanced, Stingy), shown only when the toggle is on. Generous is 25% start and +25% per gold chest, Balanced is 20% and +20%, Stingy is 15% and +15%. Gold chests give 2x, 2x, and 1.5x vanilla gold.
- **Elite relics**: Lower Elite relic rarity (toggle).
- **Gold**: Gold rate (Lean 75%, Vanilla 100%, Rich 125%).
- Presets: Off turns everything off with 3 choices and Vanilla gold. Bosses only turns on Scale bosses alone. Power curve turns on every toggle, with 4 card choices, Balanced chest odds, and Vanilla gold.
- The numbers behind the toggles are fixed in code, and the exact values live in [the enemy scaling curve](./05-enemy-scaling-curve.md) and [the treasure outcome table](./06-treasure-outcome-table.md). The 8 enemy scaling values and 3 treasure values from those tickets are now internal, not player-facing, and Gold rate's 100% stays the default.

Excluded: collapsed sections by default, because 9 rows fit without them. Showing options only after picking Custom (layout C), because tweaking a preset would take two steps. Number sliders for scaling, chest chance and gold, as too fine for the player.
