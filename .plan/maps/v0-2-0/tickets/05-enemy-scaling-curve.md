---
type: prototype
blocked_by: [01, 02]
undermined_by: []
assets: [05-enemy-scaling-curve.md, 05-scaling-curve.py, 05-enemy-scaling-curve.html]
---

# Decide the enemy scaling curve for Power curve and Bosses only

## Question

What HP and damage multipliers should enemies get at each point of a run? Power curve should rise through Acts 2 and 3 so Act 3 is the hardest stretch, aiming at Act 3 Elites and bosses averaging 4 or more turns and 15 or more damage taken. Bosses only scales bosses alone. Is the curve stepped per act, smooth along the path, or both? Are normal fights, Elites, and bosses scaled separately? Which parts become tuning values? Build a chart of candidate curves against the baseline numbers so the curve can be reacted to before any code exists.

## Answer

Use a smooth curve along the path (candidate B in [the prototype](../assets/05-enemy-scaling-curve.md)), with separate stepped values for bosses.

- Act 1 and the Act 1 boss stay at 100% in every preset, since the Notes say Power curve raises scaling through Acts 2 and 3.
- Monster, Elite, and Unknown fights share one path curve. It is 100% at the first point of Act 2 and rises linearly with the point's row fraction to the end-of-Act-2 value, then on to the end-of-Act-3 value, with no jump at the act change. Vanilla's harder Act 3 monster pool already adds that jump.
- Bosses, including a second boss, use one value per act.
- 8 tuning values. Power curve: path HP 180% / 290% and path damage 150% / 260% at the end of Act 2 / Act 3, boss HP 130% / 150% and boss damage 115% / 125% for Act 2 / Act 3. Bosses only keeps the boss values and sets the path to 100%. Off is 100% everywhere.
- Under the prototype's linear model, which holds player power at v0.1 and ignores block, Act 3 Elites project 4.0 turns and 15.6 damage, and the Act 3 boss 6.8 turns and 25 damage. The reward cuts are not modeled. If the playtest shows Act 3 overshooting, lower the end-of-Act-3 values.

Excluded: a stepped curve (A), because late-act fights get easier as the player grows through a 50 to 65 point act. Step plus ramp (C), because its act-start jump stacks on vanilla's and it needs 12 values. A separate Elite bonus, because one curve already reaches the Elite target. Scaling bosses by the path curve, because it projects about 65 damage on the Act 3 boss, above vanilla's 34 maximum.
