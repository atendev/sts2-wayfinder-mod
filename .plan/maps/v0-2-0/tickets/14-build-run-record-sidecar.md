---
type: task
blocked_by: [08, 11]
undermined_by: []
assets: []
---

# Build the run record sidecar

## Question

Write the sidecar decided in [Decide how a run records its difficulty settings](./08-record-settings-per-run.md). First check in the decompile, against the installed build, that `RunHistorySaveManager.LoadAllRunHistoryNames` ignores a `<start_time>.wayfinder.json` next to the `.run`, and that `CreateRunHistoryEntry` still has the signature the research recorded. Then add a postfix that writes the sidecar from the run snapshot built in [Build the difficulty settings screen](./11-build-difficulty-settings-screen.md): preset, every tuning value, schema version, and mod version, for Wayfinder runs only. Play a short run, then confirm `scripts/run_report.py` shows the settings and that a run with no sidecar still reads as Vanilla or Modded. Record the change in `docs/changelog.md`.
