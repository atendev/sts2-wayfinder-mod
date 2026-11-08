---
type: prototype
blocked_by: [06]
undermined_by: []
assets: [08-preview-sample-paths.py, 08-preview-sample-paths.html]
---

# Preview sample paths under the ordering rules

## Question

Using the decided ordering rules and real point counts, do generated paths for each act read as balanced when you look at a batch of them? Which rules need tightening or loosening before they get built into the mod?

## Answer

The ordering rules read as balanced for all three acts at ascension 0 and at ascension 1+. They need one change, and every other rule stays as ticket 06 decided.

Add a preference that caps Monsters and Elites together at 4 in a row. When the spread can't meet the cap, the path is built again from the same seed with no cap. The cap gives way alone, ahead of every other preference, and the build stays deterministic.

[The preview](../assets/08-preview-sample-paths.html) opens by double-click and shows batches per act, difficulty, and candidate rule. [Its script](../assets/08-preview-sample-paths.py) regenerates the page. Each case ran 1500 paths, about 52 to 60 points long on average, with mixes modeled per act from the ticket 01 table.

The verdict comes from those batch measurements and from reading 16 sample paths and 12 worst-case paths as text. The numbers below are for that 1500-path run, and a smaller run gives different worst cases.

Under the decided rules, ascension 0 and ascension 1+ gave these numbers:

- Every rule and preference held on 100.0% of paths, rounded.
- The longest stretch without a Rest site had a 95th percentile of 10 points and a worst of 14 to 17.
- The longest run of Monsters and Elites had a 95th percentile of 5 and a worst of 7 or 8. This is the one pattern that read badly, as in `MMMEMMM`, because the two types are capped separately.
- No 10 points held more than 3 Elites. The first Elite landed around position 9 to 12 on average.
- Between 24% and 31% of paths had adjacent chests.

Results for the cap, outside `BigGameHunter`:

- A strict cap misses on up to 0.3% of paths in Acts 1 and 2 at 4, and up to 0.1% at 5. A miss drops every preference and loses the spread for that whole path, which produced stretches of up to 34 points without a Rest site and up to 12 fights in a row.
- A cap that gives way first holds on at least 99.7% of paths at 4. Paths that miss it stop at 6 or 7 fights in a row and keep their spread.
- The other measures stayed put. The worst stretch without a Rest site moved by at most 2 points.
- A cap of 5 costs the same. The cap of 4 lowers the 95th percentile of fight runs from 5 to 4, and it removes shapes like `MMMEM`.

Left alone:

- Shop spacing. The stretch without a Shop has a 95th percentile of 27 to 31 points and a worst of 46 to 54. Three Shops on about 55 points cause that, and the mix keeps every point vanilla generated. In a separate simple simulation of a 60-point path, halving the jitter cut the 95th percentile from 28 to 24, too small a gain to add a rule.
- `BigGameHunter`. Only 6% to 19% of its paths meet every preference. The rest lose their spread, with a 95th percentile of 19 to 21 points without a Rest site and a worst run of up to 19 fights. The fight cap of 4 holds on only 76% to 87% of its paths, because the search that builds them has no cap. It is a run modifier whose mix can't meet the rules, and a partial spread in the fallback is more than v0.1.0 needs.

The mixes are modeled, and the in-game playthrough has to confirm them.

Excluded:

- A strict cap at any length, for the reason above.
- Halving the Shop jitter.
- A partial spread that survives dropped preferences. It would help `BigGameHunter` and nothing else.
