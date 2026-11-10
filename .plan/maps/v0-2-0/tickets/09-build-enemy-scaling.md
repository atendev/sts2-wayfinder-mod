---
type: task
blocked_by: [05, 07]
undermined_by: []
assets: []
---

# Build enemy scaling for the path curve and bosses

## Question

Build the enemy scaling decided in [Decide the enemy scaling curve](./05-enemy-scaling-curve.md), using the hooks from [Find how enemy HP and damage are set](./02-enemy-stats-and-scaling-hooks.md): a `CombatState.CreateCreature` postfix for HP and a `ModifyDamageMultiplicative` listener for damage, both gated on `UnifiedActMap.IsLine` and the settings read at run start. The settings are now three toggles (Scale bosses, Scale fight HP along the path, Scale fight damage along the path) over fixed numbers from the curve ticket, per [the settings screen](./07-difficulty-settings-screen.md); each effect runs only when its toggle was on at run start. How should scaled HP and per-hit damage round so the intent matches the real hit? Should summons and splits that spawn mid-combat get scaled, given a split can inherit an already scaled parent's HP? Should the curve read the map row at combat start or an index stored at run start? Record the change in `docs/changelog.md`.

## Answer

Built in `sts2_wayfinder_modCode/Difficulty/`: `EnemyScalingCurve` (the numbers from [the curve ticket](./05-enemy-scaling-curve.md)), `DifficultySettings` (the three toggles), and `EnemyScalingPatch` (the HP and damage patches). `EnemyScalingCurveTests` covers the curve ends and the act change. The mod builds against the installed `sts2.dll` and the 41 tests pass. Nothing was run in the game.

- **Damage hook:** a Harmony postfix on `Hook.ModifyDamage`, not a hook-listener model. A mod-defined `AbstractModel` needs `ModelDb` registration, which the research couldn't verify. The postfix sits on the one path the intent preview and the real hit both use. It applies when the dealer is an enemy monster, the attack is a powered attack, and the call includes the multiplicative phase.
- **Rounding:** HP rounds to the nearest whole number, halves away from zero. Per-hit damage is rounded inside the postfix, after every other modifier, so the intent's `(int)` truncation always sees a whole number and matches the hit.
- **Summons and splits:** only monsters in the encounter's own `MonstersWithSlots` get scaled HP. Mid-combat spawns keep vanilla HP, which also keeps a split from scaling an already scaled parent twice. Their attacks still scale, because damage keys on the dealer being an enemy.
- **Curve position:** read live from `CurrentMapCoord` and `BossMapPoint` at combat start (HP) and at each hit (damage), with no index stored at run start. A mid-combat reload restores the same coord, so nothing needs saving.
- **Gating:** `UnifiedActMap.IsLine` plus the three toggles. Until [Build the difficulty settings screen](./11-build-difficulty-settings-screen.md) lands, `DifficultySettings.EnemyScaling` is a static defaulting to all three on, so every Wayfinder run is Power curve. That build ticket replaces it with the run-start snapshot.

Excluded: a hook-listener model (registration unverified), scaling summons (double-scales splits), and a stored curve index (needs saving for no gain).

Not verified: that `Hook.ModifyDamage` is called with the multiplicative flag from both the intent and the hit in the installed build, and the in-game numbers. The playtest ticket covers both.
