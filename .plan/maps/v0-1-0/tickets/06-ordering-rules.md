---
type: grilling
blocked_by: [01, 04]
undermined_by: []
assets: [06-ordering-stress-test.py]
---

# Define the ordering rules

## Question

What ordering rules place points on the unified path? Which are hard rules and which are preferences that can bend when the point mix makes them impossible? Starting cases: no two elites back to back, no long runs of treasure, how early elites may appear, how rest sites and shops are spaced, and what sits directly before the boss. How much randomness should remain between runs on the same rules, and what happens when no valid order exists?

## Answer

These rules order the points between the Ancient and the boss. Positions count from 1, the first point after the Ancient, so a point's position equals its map row. A second boss stays after the first and is not part of the ordering.

Hard rules:

- Position 1 is a Monster, and the last point is a Rest site. Each applies whenever the act's mix holds that type.
- Positions 1 to 5 hold no Elite, Rest site, or Treasure. A short path's fixed last Rest site is exempt.
- The two points before the last point are not Rest sites.
- No two Elites, Rest sites, or Shops sit back to back. These are the types vanilla keeps apart, except Treasure, because adjacent chests are fun.
- At least 2 other points sit between any two Elites.
- No more than 3 Treasures sit in a row. The map's goal rules out long chest runs, and a wide treasure row can hold up to 7.

Preferences:

- Elites, Rest sites, and Shops spread roughly evenly across the positions open to them, with seeded jitter on each position. Shops can start at position 2, because the opening ban doesn't cover them. Elites and Rest sites start at position 6.
- Treasure is not spread. It lands at random from position 6 on, so chests sometimes touch.
- No more than 3 Monsters in a row and no more than 3 Unknowns in a row.

The same seed always gives the same path. Different seeds change the jitter, the treasure positions, and the Monster and Unknown shuffle, and they keep the same pacing.

Ordering never fails and always places every point. When a mix can't satisfy everything, rules break in this fixed order, first to break at the top:

1. Preferences.
2. No Rest site in the two points before the last point.
3. No back-to-back Rest sites or Shops.
4. The Treasure run cap.
5. The Elite gap, shrinking from 2 other points to 1, then to 0, which still keeps Elites from touching.
6. The opening ban on positions 1 to 5.
7. No back-to-back Elites.
8. Position 1 is a Monster, and the last point is a Rest site. This step is listed for completeness and never breaks, because these rules apply only when the mix holds the type.

After finding the first step that gives a valid order, the engine restores each earlier hard rule it dropped, most important first, and keeps any the mix still satisfies. Only rules that really conflict stay broken.

[The stress test](../assets/06-ordering-stress-test.py) modeled these rules over 500 paths per scenario. It had no failures, the same seed always repeated its path, and a brute-force check of 400 small mixes agreed with the solver.

- Vanilla mixes with 5 or 8 elites all ordered at the first level, which applies every preference while building the path. The validator checks hard rules only, and it found none broken. The longest stretch without a Rest site averaged about 8 points (worst 16). The longest run of fights averaged 3.6 (worst 7). About 28% of paths had adjacent chests, and never more than 3.
- `BigGameHunter` mixes ordered at the first level in 12% of paths. The Elite gap broke in 86%, the opening ban in 12%, and Elites touched in 6%.
- Step 8 is never needed. Once every other rule is gone, nothing stops a mix that holds a Monster or a Rest site from putting it first or last. The model pins those two points and has no level for this step.

The mixes are modeled. Real point counts and the `BigGameHunter` conversion are inferred from the decompile, so the in-game playthrough has to confirm them.

For the build: the search needs an early check that the remaining Elites, Rest sites, and Shops still fit in the open positions. An earlier run of the model, before that check was added, took 100 to 200 times longer per path on elite-heavy mixes, exhausted its search budget, and broke rules it didn't need to. When preferences drop, the spread is lost for the whole path, which only affects mixes that already can't meet everything.

Excluded:

- Picking uniformly from all valid orders. In a separate throwaway simulation of 60-point paths, not part of the committed script, the longest stretch without a Rest site reached 26 points, and runs of fights reached 14.
- Hand-written pacing templates.
- Scaling the opening ban to path length. It would push every Elite into the last two thirds of the path, even though the player's deck at position 6 matches vanilla's at row 6.
- Banning adjacent Treasure, or spreading it evenly, which made adjacent chests happen in 0% of paths in that same throwaway simulation.
- Leaving a vanilla map in place, or throwing, when no valid order exists. The destination says every act is replaced.
- The plain break order with no restoring. It put Rest sites or Shops back to back in 38% of `BigGameHunter` paths for no reason. Set `RESTORE_RULES` to `False` in the script to reproduce this.
- A combined cap on fights. The preview ticket can revisit it after seeing a batch of paths.
