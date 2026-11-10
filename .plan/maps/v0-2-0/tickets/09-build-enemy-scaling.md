---
type: task
blocked_by: [05, 07]
undermined_by: []
assets: []
---

# Build enemy scaling for the path curve and bosses

## Question

Build the enemy scaling decided in [Decide the enemy scaling curve](./05-enemy-scaling-curve.md), using the hooks from [Find how enemy HP and damage are set](./02-enemy-stats-and-scaling-hooks.md): a `CombatState.CreateCreature` postfix for HP and a `ModifyDamageMultiplicative` listener for damage, both gated on `UnifiedActMap.IsLine` and the settings read at run start. The settings are now three toggles (Scale bosses, Scale fight HP along the path, Scale fight damage along the path) over fixed numbers from the curve ticket, per [the settings screen](./07-difficulty-settings-screen.md); each effect runs only when its toggle was on at run start. How should scaled HP and per-hit damage round so the intent matches the real hit? Should summons and splits that spawn mid-combat get scaled, given a split can inherit an already scaled parent's HP? Should the curve read the map row at combat start or an index stored at run start? Record the change in `docs/changelog.md`.
