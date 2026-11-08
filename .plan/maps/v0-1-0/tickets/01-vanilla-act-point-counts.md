---
type: research
blocked_by: []
undermined_by: []
assets: [01-vanilla-act-point-counts.md]
---

# Measure what a vanilla act generates

## Question

How many points does a vanilla act generate, by type and by act? What are the grid width, the row count per act, and the number of paths the generator draws? Which rows does `AssignPointTypes` fix to a type, such as the first row, the treasure row, and the row before the boss? How do the second boss, `ShouldReplaceTreasureWithElites`, and ascension change the counts? What are the placement rules behind `IsValidPointType`?

## Answer

Findings are in [the point counts research](../assets/01-vanilla-act-point-counts.md).

The grid is 7 columns wide with 7 paths drawn. Acts generate 13 to 15 rows. The first row is all Monster, the row at `rooms - 6` is all Treasure, and the last row is all RestSite. Targets are 3 shops, 5 elites (8 at ascension 1), 5 to 7 rests, and 9 to 14 unknowns. The second boss appears only in the last act at ascension 10, and `ShouldReplaceTreasureWithElites` is always false in vanilla.

The total point count is random and depends on pruning, so no exact figure exists. A simulation that ignores pruning gives a mean of about 57 to 66 points. That figure is inferred, and the real count needs measuring in-game or by running the generator.
