# Enemy scaling curve candidates

Prototype for [Decide the enemy scaling curve](../tickets/05-enemy-scaling-curve.md). [05-scaling-curve.py](./05-scaling-curve.py) holds the candidates, prints the projection tables below, and writes [05-enemy-scaling-curve.html](./05-enemy-scaling-curve.html), which charts the HP and damage multipliers across the run with a hover readout.

```
python3 .plan/maps/v0-2-0/assets/05-scaling-curve.py
```

## Projection model

From the [v0.1 baseline](./00-v0-1-baseline.md), both Wayfinder runs pooled and weighted by fight count:

- turns = baseline turns × the act's mean HP multiplier
- damage taken = baseline damage × mean HP multiplier × mean damage multiplier

Player power is held at v0.1 levels and block is ignored. That makes it a rough estimate of what scaling alone does. The card reward, treasure, and Elite relic changes in Power curve cut player power on top of it, and nothing here models them.

## Candidates

All three keep Act 1 at 100%, per the map Notes ("raises enemy scaling through Acts 2 and 3"). All three are tuned to the same Act 3 mean on the path (HP 2.35×, damage 2.05×), so they differ in shape, not strength.

| Candidate | Shape | Path tuning values per stat |
| --- | --- | --- |
| A. Stepped per act | One flat multiplier for Act 2 and one for Act 3 | 2 |
| B. Smooth along the run | Rises linearly from 100% at the start of Act 2 to a value at the end of Act 2, then on to a value at the end of Act 3, with no jump at the act change | 2 (end of Act 2, end of Act 3) |
| C. Step plus ramp | Its own start and end for Act 2 and for Act 3, so it jumps at each act change | 4 |

Bosses are stepped in every candidate. There is one boss per act, so smooth and stepped mean the same thing for bosses. Boss values are 100/130/150% HP and 100/115/125% damage for Acts 1/2/3.

## Act 3 projection

Target: 4 or more turns and 15 or more damage taken.

| Curve | Fight | Baseline turns | Baseline dmg | HP × | Dmg × | Projected turns | Projected dmg | Target |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A, B, C | Monster | 1.5 | 1.6 | 2.35 | 2.05 | 3.4 | 7.8 | n/a |
| A, B, C | Elite | 1.7 | 3.2 | 2.35 | 2.05 | 4.0 | 15.6 | meets |
| A, B, C | Boss | 4.5 | 13.5 | 1.50 | 1.25 | 6.8 | 25.3 | meets |

Vanilla Act 3 for comparison: Monsters 2.8 to 6 turns and 3.5 to 17 damage, Elites 4 to 8 turns and 4 to 45 damage, bosses 7 to 12 turns and 0 to 34 damage.

Every act, Elites and bosses:

| Curve | Act | Elite turns | Elite dmg | Boss turns | Boss dmg |
| --- | --- | --- | --- | --- | --- |
| A, B, C | 1 | 4.1 | 10.6 | 6.0 | 22.5 |
| A | 2 | 2.9 | 7.3 | 2.6 | 6.7 |
| B | 2 | 2.8 | 6.8 | 2.6 | 6.7 |
| C | 2 | 2.9 | 7.3 | 2.6 | 6.7 |
| A, B, C | 3 | 4.0 | 15.6 | 6.8 | 25.3 |

## Reading

- Under this model, scaling alone gets Act 3 Elites to about 4 turns (4.04) and about 16 damage, just over the target. The reward cuts come on top and are not modeled. If the playtest shows Act 3 overshooting once they land, lower the end-of-Act-3 values rather than the shape. That offset is an assumption for the playtest to check.
- Ignoring block likely understates damage. Once enemy hits outgrow the player's block, damage taken rises faster than the multiplier, so 15.6 may be low for strong curves.
- One curve covers Monsters and Elites. It brings Act 3 Monsters to about 3.4 turns, inside the vanilla range, and Elites already reach the target, so Elites don't need their own bonus.
- Bosses need far less than the path. Scaling the Act 3 boss by the path's 2.35× HP and 2.05× damage would project about 11 turns, within vanilla's 7 to 12, and 65 damage, above vanilla's 34 maximum. So bosses get their own, smaller values.
- The Act 2 boss stays short (2.6 turns). The baseline's Act 2 bosses took 1 and 3 turns, and the target only covers Act 3, so this is left to the playtest.
- A, B, and C land at the same Act 3 means. They differ in how Act 2 feels and where the jumps fall:
  - A jumps at each act start and stays flat while the player keeps getting stronger through a 50 to 65 point act, so late-act fights get easier again.
  - C tracks power within each act, but the jump at each act start stacks on vanilla's own jump to the next act's stronger monster pool.
  - B tracks the player's growth along the path and leaves the act-start jump to vanilla's monster pools. It ties A for the fewest values.

## Recommendation: B, smooth along the run

| Tuning value | Off | Bosses only | Power curve |
| --- | --- | --- | --- |
| Enemy HP at end of Act 2 | 100% | 100% | 180% |
| Enemy HP at end of Act 3 | 100% | 100% | 290% |
| Enemy damage at end of Act 2 | 100% | 100% | 150% |
| Enemy damage at end of Act 3 | 100% | 100% | 260% |
| Boss HP, Act 2 / Act 3 | 100% | 130 / 150% | 130 / 150% |
| Boss damage, Act 2 / Act 3 | 100% | 115 / 125% | 115 / 125% |

- Act 1 and the Act 1 boss stay at 100% in every preset.
- The path curve is 100% at the first point of Act 2 and moves linearly to each end-of-act value, using the point's row fraction, `(row - 1) / (N - 1)`, from the hooks research. Act 3 starts where Act 2 ended.
- Monster, Elite, and Unknown fights use the path curve. Bosses, including the second boss under Double Boss, use the boss value for their act.
- Bosses only sets the path curve to 100% and keeps the Power curve boss values.
- That gives 8 tuning values: 4 for the path and 4 for bosses.

## Left for the build ticket

- Rounding. Round scaled HP to the nearest whole number. Round damage per hit inside the hook so the intent and the real hit agree. The hooks research did not confirm how `CreatureCmd` rounds.
- Mid-combat spawns (summons, splits). A `CreateCreature` postfix also catches them. A split that takes its HP from an already scaled parent would get scaled twice, so the build ticket should decide this per spawn path.
- Whether the curve reads the map row at combat start or a point index stored at run start. The row is reload-safe on a Wayfinder map, per the hooks research.
