---
type: task
blocked_by: [04, 07, 08]
undermined_by: []
assets: []
---

# Build the difficulty settings screen

## Question

Build the screen decided in [Decide the difficulty settings screen](./07-difficulty-settings-screen.md) on `WayfinderConfig`, using the BaseLib facts from [Find how BaseLib mod config can show presets and tuning values](./04-baselib-config-and-run-record.md): enum and bool properties, `[ConfigSection]`s, `[ConfigVisibleIf]` for Chest odds, and `ConfigChanged` plus `ConfigReloaded()` with a re-entrancy guard for presets and Custom. Add the labels, hover tips, and enum names to `settings_ui.json`. Snapshot the options at run start and save them in the run through the route chosen in [Decide how a run records its difficulty settings](./08-record-settings-per-run.md), so every consumer reads the snapshot. Check that applying a preset doesn't bounce the dropdown back to Custom, and that a run without saved settings reads as Off. Record the change in `docs/changelog.md`.
