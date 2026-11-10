---
type: task
blocked_by: [08, 11]
undermined_by: []
assets: []
---

# Build the run record sidecar

## Question

Write the sidecar decided in [Decide how a run records its difficulty settings](./08-record-settings-per-run.md). First check in the decompile, against the installed build, that `RunHistorySaveManager.LoadAllRunHistoryNames` ignores a `<start_time>.wayfinder.json` next to the `.run`, and that `CreateRunHistoryEntry` still has the signature the research recorded. Then add a postfix that writes the sidecar from the run snapshot built in [Build the difficulty settings screen](./11-build-difficulty-settings-screen.md): preset, every tuning value, schema version, and mod version, for Wayfinder runs only. Play a short run, then confirm `scripts/run_report.py` shows the settings and that a run with no sidecar still reads as Vanilla or Modded. Record the change in `docs/changelog.md`.

## Answer

Built, with the sidecar moved to a subfolder. The check this ticket asked for failed: `RunHistorySaveManager.LoadAllRunHistoryNames` lists every file in `history/` except those ending in `.corrupt` or `.backup`, and `GetHistoryCount` counts them all, so `history/<start_time>.wayfinder.json` would have been loaded as a run and failed. The sidecar is now `history/wayfinder/<start_time>.json`; the file store lists one folder level only, so the game never sees it. `CreateRunHistoryEntry(SerializableRun, bool, bool, PlatformType)` has the signature the research recorded.

`RunRecordPatch` is a postfix on it. It reads the snapshot from `RunManager.DebugOnlyGetState()` and writes `RunRecordSidecar.ToJson`: `schema_version`, `mod_version`, `preset`, then every tuning value. It writes for Wayfinder runs only, the Off preset included, and logs a warning instead of throwing. `scripts/run_report.py` now checks `history/wayfinder/<stem>.json` first and the old adjacent `<stem>.wayfinder.json` second. The changelog has the entry.

Not done: a run abandoned from the main menu has no run state to read the snapshot from, so it gets no record. The mod build passes, the 84 xUnit tests and 16 report tests pass, but no run was played in-game, so the written file and the report reading it against a real `.run` are unconfirmed. `RunRecordSidecar.ModVersion` is a constant and must be bumped with the manifest.
