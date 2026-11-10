---
type: grilling
blocked_by: [04]
undermined_by: []
assets: []
---

# Decide how a run records its difficulty settings

## Question

The `.run` history file copies only a fixed list of fields, so the difficulty settings a run used don't reach it on their own. Which route should Wayfinder use so the run file reader can tell which preset and tuning values a run had: a Harmony patch that adds a field to the run history, a sidecar file next to the history, or a card, relic, or modifier that carries the values? Weigh what each costs if the game updates its history format, whether a run without the mod still loads, and whether the player sees anything.

## Answer

Use a **sidecar file**. At run start the mod snapshots the difficulty settings into the run save (`current_run.save`) through BaseLib's extended run save data on `IRunState`, and every consumer reads that snapshot, never the config. A missing snapshot reads as Off. When the run is written to history, a postfix on `RunHistoryUtilities.CreateRunHistoryEntry` writes `history/<start_time>.wayfinder.json` from the snapshot, which is where `scripts/run_report.py` already looks.

The sidecar holds the preset name, every tuning value (whole-number percents, toggles, and named choices), a schema version, and the mod version. It is written only for Wayfinder runs, including runs on the Off preset, so a run with no sidecar reads as Vanilla or Modded. The player sees nothing in the game.

The sidecar leaves the `.run` untouched, so a run loads the same without the mod, and a game update to the history format at worst moves the postfix hook. Before building, check that `RunHistorySaveManager.LoadAllRunHistoryNames` ignores the extra file.

Excluded: patching `RunHistory` through `MegaCritSerializerContext.RunHistoryPropInit` (breaks if the generated serializer changes, and an unknown top-level key might not load in vanilla), a modifier carrier (changes Neow and the top bar), a relic or card carrier (repeats data and needs a hidden item), and showing the record on a game screen.

Build: [Build the run record sidecar](./14-build-run-record-sidecar.md). The snapshot itself is part of [Build the difficulty settings screen](./11-build-difficulty-settings-screen.md).
