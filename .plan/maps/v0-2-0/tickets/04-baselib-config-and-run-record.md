---
type: research
blocked_by: []
undermined_by: []
assets: [04-baselib-config-and-run-record.md]
---

# Find how BaseLib mod config can show presets and tuning values

## Question

How does BaseLib's mod config work, starting from the existing EnableWayfinder toggle? What control types does it support, such as toggles, sliders, number fields, dropdowns, and groups or sections? Can choosing a preset in a dropdown set several other values, and can changing one of those values switch the dropdown to Custom? Where are config values stored (`mod_configs/`), and how does the mod read them only when a run starts? Can a run keep its difficulty settings in the run save so a loaded run uses the same values, and can the mod write them into the `.run` history file or some other per-run record the run file reader can find?

## Answer

BaseLib config supports bool tickboxes, int/float/double sliders, enum dropdowns, string line edits, color pickers, buttons, collapsible sections, and conditional row visibility. A preset dropdown can set other values by handling `ConfigChanged` on the config class and calling `ConfigReloaded()` to refresh the controls, and editing a tuning value can flip the preset to Custom the same way, with a re-entrancy guard. Values are static properties saved as strings in `mod_configs/sts2_wayfinder_mod.cfg` and are live in memory, so the mod should snapshot them in a run-start hook. A run can keep its settings in `current_run.save` through BaseLib's extended run save data (`ExtendedSaveTypes.RegisterSavedValue` with `IRunState`), but the `.run` history copies only a fixed field list, so writing settings there needs a Harmony patch on the history writer, a sidecar file, or a modifier carrier.

Details and sources: [assets/04-baselib-config-and-run-record.md](../assets/04-baselib-config-and-run-record.md).
