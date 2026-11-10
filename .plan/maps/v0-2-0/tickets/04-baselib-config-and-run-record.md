---
type: research
blocked_by: []
undermined_by: []
assets: []
---

# Find how BaseLib mod config can show presets and tuning values

## Question

How does BaseLib's mod config work, starting from the existing EnableWayfinder toggle? What control types does it support, such as toggles, sliders, number fields, dropdowns, and groups or sections? Can choosing a preset in a dropdown set several other values, and can changing one of those values switch the dropdown to Custom? Where are config values stored (`mod_configs/`), and how does the mod read them only when a run starts? Can a run keep its difficulty settings in the run save so a loaded run uses the same values, and can the mod write them into the `.run` history file or some other per-run record the run file reader can find?
