---
type: task
blocked_by: [04, 07, 08]
undermined_by: []
assets: []
---

# Build the difficulty settings screen

## Question

Build the screen decided in [Decide the difficulty settings screen](./07-difficulty-settings-screen.md) on `WayfinderConfig`, using the BaseLib facts from [Find how BaseLib mod config can show presets and tuning values](./04-baselib-config-and-run-record.md): enum and bool properties, `[ConfigSection]`s, `[ConfigVisibleIf]` for Chest odds, and `ConfigChanged` plus `ConfigReloaded()` with a re-entrancy guard for presets and Custom. Add the labels, hover tips, and enum names to `settings_ui.json`. Snapshot the options at run start and save them in the run through the route chosen in [Decide how a run records its difficulty settings](./08-record-settings-per-run.md), so every consumer reads the snapshot. Check that applying a preset doesn't bounce the dropdown back to Custom, and that a run without saved settings reads as Off. Record the change in `docs/changelog.md`.

## Answer

Built. `WayfinderConfig` now has the preset dropdown under Enable Wayfinder, then sections for Enemy scaling, Card rewards (Card choices is a 3 to 5 slider), Treasure chests (Chest odds shown only while chances are on), Elite relics, and Gold, with labels and hover tips in `settings_ui.json`. `ConfigChanged` applies a preset to every option, or flips the dropdown to Custom when an option moves off the preset, behind a re-entrancy guard and followed by `ConfigReloaded()`. A hand-edited cfg that disagrees with its preset loads as Custom.

`DifficultyOptions` is pure and unit-tested. It derives the preset by matching options (a hidden Chest odds choice never makes a run Custom) and round-trips as JSON, reading a missing or broken snapshot as Off. `SnapshotDifficultyPatch` copies the options into the run in a prefix on `RunManager.SetUpNewSingleplayer` and `SetUpNewMultiplayer`, only when Wayfinder is on. The snapshot is saved in `current_run.save` through `ExtendedSaveTypes.RegisterSavedValue<IRunState, string>` as one JSON string. Enemy scaling and the treasure patch now read `DifficultySettings.For(runState)`.

Not done: the "Custom: options no longer match a preset" banner. BaseLib has no banner control, so the text is in the preset's hover tip instead. Not run in the game, so the two checks this ticket asked for (applying a preset doesn't bounce to Custom, and a reloaded run reads its snapshot) are for the playtest ticket. The card choices, alternation, Elite rarity and gold options are saved but nothing reads them until their build tickets land.
