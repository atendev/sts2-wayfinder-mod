---
type: task
blocked_by: []
undermined_by: []
assets: [00-v0-1-baseline.md, 01-run-reader-draft.py]
---

# Commit the run file reader and the v0.1 baseline

## Question

Turn the draft reader in `assets/01-run-reader-draft.py` into a committed script under `scripts/` that takes one or more `.run` files, or a history folder, and prints the per-act and by-point-type tables in `assets/00-v0-1-baseline.md`. It should find the history folders on its own when given no arguments, separate Wayfinder and vanilla runs, and print the preset and tuning values a run used once later tickets record them. Regenerating the baseline with the committed script should reproduce the numbers in the asset. The draft has an unused helper and only counts picked relics, so check that relics from events and shops show up as expected.

## Answer

`scripts/run_report.py` is committed with 15 tests in `tests/run_report/`. Run it on the six baseline runs and it reproduces every row in [the v0.1 baseline](../assets/00-v0-1-baseline.md). The asset now shows that command.

- Input: `.run` files or history folders. With no arguments it finds every `steam/*/profile*` and `steam/*/modded/profile*` history folder. Output is the same Markdown tables as the baseline, plus Rest site, Ancient, and Boss rows. `--mode` and `--min-acts` filter runs.
- Wayfinder or vanilla: the modded folder also holds copies of vanilla runs, so the folder can't decide alone. A run is Wayfinder if it records difficulty settings or has an act over 18 points. A vanilla-folder run, or a modded copy whose vanilla twin exists, is Vanilla. Anything else is Modded, which means Wayfinder off or a run that died before the map showed.
- Settings: read from a top-level `wayfinder` object in the `.run` or a `<start_time>.wayfinder.json` sidecar, so either option in [Decide how a run records its difficulty settings](./08-record-settings-per-run.md) works without changing the reader. Settings print as a preset plus tuning values.
- Relics: a bought relic is also a picked `relic_choices` entry, and event relics are picked entries too, so counting picked relics already includes shops and events. A "Relic check" table prints when starting relics plus picked minus `relics_removed` misses the final count. All 64 runs on disk reconcile.
- The baseline's "Cards picked" column was `cards_gained`, so it is now named "Cards gained".

Excluded: a changelog entry, since this is tooling and not a mod change.
