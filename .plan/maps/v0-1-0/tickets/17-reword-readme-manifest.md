---
type: task
blocked_by: []
undermined_by: []
assets: []
---

# Reword the README and manifest for each act

## Question

The README and `sts2_wayfinder_mod.json` say "a single act" where the mod means each act individually. Reword both.

## Answer

Both now say every point in each act goes into one unified path "for that act". The added phrase stops the line reading as one path for the whole run. Both also say "point" instead of "encounter", as the map's vocabulary note requires, and `docs/changelog.md` records the change.

Excluded: the "combinding" typo and "encounters of the spire" in the README, which are outside the "single act" wording.

Verified by JSON parse of the manifest and a search for "single act" in both files. Neither text has been seen in-game.
