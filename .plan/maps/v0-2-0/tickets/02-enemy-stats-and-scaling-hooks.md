---
type: research
blocked_by: []
undermined_by: []
assets: [02-enemy-stats-and-scaling-hooks.md]
---

# Find how enemy HP and damage are set and where scaling can hook in

## Question

How does Slay the Spire 2 set a monster's max HP and the damage of its attacks, and where does ascension already raise them? Which methods or hooks could multiply HP and damage for one combat based on the act and the point's position on the path, for normal fights, Elites, and bosses separately? Does any existing mechanism, such as an ascension modifier, a run modifier, or a power applied at combat start, already do this and could be reused? Does scaled damage show correctly in the intent shown above the enemy? What does a mod need to know in a combat about where on the unified path the player is?

## Answer

Each monster class hard-codes its own HP and damage numbers, with ascension (ToughEnemies for HP, DeadlyEnemies for damage) as a fixed per-monster switch, so ascension cannot be reused as a multiplier and no run modifier scales enemies either. HP can be scaled by a Harmony postfix on CombatState.CreateCreature (set max HP, then current HP), which sees the encounter room type (Monster, Elite, Boss), the act index, and the point row. Damage can be scaled by a hook listener with ModifyDamageMultiplicative for enemy dealers, registered through ModHelper.SubscribeForCombatStateHooks or applied as a power like the Murderous modifier does. Because intents call Hook.ModifyDamage, scaled damage shows correctly in the intent. Position on the path is readable in combat from CurrentActIndex, CurrentMapCoord.row, and UnifiedActMap.IsLine.

Details, signatures, and unverified points: [02-enemy-stats-and-scaling-hooks.md](../assets/02-enemy-stats-and-scaling-hooks.md)
