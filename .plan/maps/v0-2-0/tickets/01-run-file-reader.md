---
type: task
blocked_by: []
undermined_by: []
assets: [00-v0-1-baseline.md, 01-run-reader-draft.py]
---

# Commit the run file reader and the v0.1 baseline

## Question

Turn the draft reader in `assets/01-run-reader-draft.py` into a committed script under `scripts/` that takes one or more `.run` files, or a history folder, and prints the per-act and by-point-type tables in `assets/00-v0-1-baseline.md`. It should find the history folders on its own when given no arguments, separate Wayfinder and vanilla runs, and print the preset and tuning values a run used once later tickets record them. Regenerating the baseline with the committed script should reproduce the numbers in the asset. The draft has an unused helper and only counts picked relics, so check that relics from events and shops show up as expected.
