# BaseLib config and per-run record

Sources: mod code in `sts2_wayfinder_modCode/`; BaseLib tag v3.4.7 at https://github.com/Alchyr/BaseLib-StS2 (paths below are relative to that repo, tag `v3.4.7`, the version in `sts2_wayfinder_mod.json` and the installed nupkg at `~/.nuget/packages/alchyr.sts2.baselib/3.4.7`); config files in `~/.local/share/SlayTheSpire2/mod_configs/`; `references/decompiled-v0.107.1/`; sample `.run` file `1794162390.run`. The decompile is v0.107.1, so recheck signatures against the installed build before building.

## 1. The existing EnableWayfinder config

- `sts2_wayfinder_modCode/WayfinderConfig.cs`: `internal class WayfinderConfig : SimpleModConfig` with one `[ConfigHoverTip] public static bool EnableWayfinder { get; set; } = true;`.
- `MainFile.cs` `Initialize()` calls `ModConfigRegistry.Register(ModId, new WayfinderConfig())`.
- Labels and hover text come from `sts2_wayfinder_mod/localization/eng/settings_ui.json` (keys `STS2_WAYFINDER_MOD-ENABLE_WAYFINDER.title`, `.hover.title`, `.hover.desc`).
- Where it is read: `UnifiedPath/RewriteMapPatch.cs` `IsWayfinderRun(IRunState)` reads `WayfinderConfig.EnableWayfinder` when the run's map is still a `NullActMap` and act index is 0 (that is, at first map generation). After that, a run is Wayfinder only if `UnifiedActMap.IsLine(runState.Map)`, so the saved map shape is what makes a loaded run stay Wayfinder. `UnifiedPath/ExtendEncounterListPatch.cs` `ScaleCount` also reads the static each time it runs. Nothing snapshots the value into the run, so today a toggle change mid-run could still affect `ScaleCount`.

## 2. How BaseLib config works

All paths in BaseLib repo at v3.4.7.

- Base classes: `Config/ModConfig.cs` (abstract `ModConfig`) and `Config/SimpleModConfig.cs` (`SimpleModConfig : ModConfig`, auto-builds the UI by reflection). Registry: `Config/ModConfigRegistry.cs` (`Register(string modId, ModConfig)`, `Get(modId)`, `Get<T>()`, `GetAll()`).
- Config properties are `public static` properties on the config class. Reflection finds them (`CheckConfigProperties`), and the constructor `Init()` records the initializer values as defaults, then calls `Load()` if the file exists or `Save()` to write defaults.
- Storage: `Config/ModConfig.cs` constructor sets `_path = OS.GetUserDataDir()/mod_configs/<root namespace, special chars stripped>.cfg`. A filename can be passed to the constructor. On disk this is `~/.local/share/SlayTheSpire2/mod_configs/sts2_wayfinder_mod.cfg`, a flat JSON object of `string -> string`: `{ "EnableWayfinder": "True" }`. Values are converted with `TypeDescriptor.GetConverter(property.PropertyType)` (`TryApplyPropertyValue`), so int, float, double, bool, string, enums and Godot `Color` all round-trip as strings. Missing keys keep their default and trigger a re-save. A broken file is moved to `<name>.cfg.broken`.
- Save timing: `Save()` writes via temp file. The config submenu (`Config/UI/NModConfigSubmenu.cs`, `SaveCurrentConfig`) saves when the screen is closed, `NGameQuitModConfigSavePatch` saves on quit, and `SaveDebounced(delayMs)` exists. Code that changes a property must call one of these or the change is only in memory.
- Reading at run start: the config is just static properties, always current in memory (loaded at mod init, updated live by the UI). BaseLib has no "run start" concept, so the mod must read the statics at a run-start hook and copy them into a snapshot. Good hook candidates in the decompile: `RunManager.SetUpNewSingleplayer(RunState, bool, DateTimeOffset?)` and `SetUpNewMultiplayer(...)` for new runs; `RunManager.SetUpSavedSingleplayer(RunState, SerializableRun)` for loaded runs; `RunState.CreateForNewRun(...)`. The existing mod lazily reads at first map generation (`RewriteMapPatch`).

### Control types (from `SimpleModConfig.GenerateOptionFromProperty` and `Config/ConfigAttributes.cs`)

| Property type | Control | Notes |
|---|---|---|
| `bool` | Tickbox (`NConfigTickbox`) | |
| `int`, `float`, `double` | Slider (`NConfigSlider`, `SupportedTypes`) | `[ConfigSlider(min, max, step, Format = "{0}%")]`; negatives and decimals allowed; a `.sliderFormat` loc key overrides Format |
| `enum` | Dropdown (`NConfigDropdown`) | one item per enum value; label from loc key `<PREFIX>-<SLUG_PROPERTY>.<EnumValue>` in `settings_ui`, falling back to the enum name; `[ConfigDropdownOverrideLocalizationAttribute]` shares loc keys |
| `string` | Line edit (`NConfigLineEdit`) | `[ConfigTextInput]` with preset or regex and max length |
| `Godot.Color` or `string` + `[ConfigColorPicker]` | Color picker | |
| method | Button | `[ConfigButton("LOC_KEY")]` on a method |

There is no plain number text field except the line edit on a string; sliders cover numbers.

Layout and behavior attributes: `[ConfigSection("Name")]` starts a collapsible section (`NConfigCollapsibleSection`, `CollapsedByDefault`), applying to the property it decorates and every following property until the next section attribute. Loc key `<PREFIX>-<SECTION_SLUG>.title`. `[ConfigHoverTip]` / `[ConfigHoverTipsByDefault]`, `[ConfigVisibleIf(nameof(Prop), args...)]` (also accepts a bool method; `Invert = true`), `[ConfigHideInUI]` (saved, no UI), `[ConfigIgnore]` (neither saved nor shown), `[ConfigIgnoreRestoreDefaults]`. Properties show in source order. A `Restore Defaults` button is added automatically.

### Presets that set other values, and Custom

BaseLib has no built-in preset feature, but the hooks to build one exist:

- Every UI control writes the static property then calls `ModConfig.Changed()`, which raises `public event EventHandler? ConfigChanged`. Subscribe in the config class (for example in its constructor, after `base()`): when `Preset` changed to something other than Custom, assign the preset's numbers to the other static properties; when any tuning property changed and it no longer matches the active preset's numbers, set `Preset = Custom`.
- Controls do NOT refresh themselves when code sets a property. Each control subscribes to `public event Action? OnConfigReloaded` and refreshes in `SetFromProperty`. After setting several properties from code, call `ConfigReloaded()` (public on `ModConfig`) so the dropdown, sliders, and tickboxes redraw. `SimpleModConfig` itself calls `ConfigReloaded()` after Restore Defaults (line ~271).
- Visibility of rows re-evaluates on `ConfigChanged`, so `[ConfigVisibleIf(nameof(Preset), Preset.Custom)]` can show tuning rows only for Custom, or you can show them always.
- Loop risk: `NConfigSlider.SetValue` uses `_slider.SetValueWithoutAnimation(...)` (does not call `Changed()` itself); the `ValueChanged` signal path `OnValueChanged` is what calls `Changed()`. I did not confirm that `SetValueWithoutAnimation` suppresses the signal, so guard the handler with a re-entrancy flag (`_applyingPreset`) and check during a playtest that applying a preset does not bounce back to Custom.
- A preset change from a code write that bypasses the UI (for example editing the cfg file) is picked up by `Load()` but fires neither event; if Custom detection matters at load time, also compare values in the constructor after `base()`.
- Reading the dropdown: `Preset` is a plain static enum property; its stored form in the cfg is the enum name, such as `"PowerCurve"`.

Suggested shape (not mod code, a sketch of types):

```csharp
internal enum DifficultyPreset { Off, BossesOnly, PowerCurve, Custom }
internal class WayfinderConfig : SimpleModConfig {
    public static bool EnableWayfinder { get; set; } = true;
    [ConfigSection("Difficulty")] public static DifficultyPreset Preset { get; set; } = DifficultyPreset.PowerCurve;
    [ConfigSlider(0, 300, 5, Format = "{0}%")] public static int EnemyHpPercent { get; set; } = 100;
    // ... one static property per tuning value, ints/bools/enums only (see section 3)
}
```

## 3. Keeping difficulty settings in the run save

Two separate files matter: the in-progress save (`current_run.save`, type `SerializableRun`) and the history file (`history/<start_time>.run`, type `RunHistory`).

### In-progress save: yes, via BaseLib extended save data

- `Patches/Saves/ExtendedSaveHandlers.cs` defines `ExtendedSaveHandlers<DataType, SerializableType>` and `ExtendedSavePatches`, which adds JSON properties to `SerializableCard`, `SerializableRelic`, `SerializablePotion`, `SerializableReward`, `SerializablePlayer`, and `SerializableRun` (with `DataType = IRunState`). Patched hooks: `RunManager.ToSave` postfix (copies state into `SerializableRun`), `RunManager.CanonicalizeSave`, `RunState.FromSerializable` postfix (calls `ExtendedSaveHandlers<IRunState, SerializableRun>.Load`), `SerializableRun.Serialize/Deserialize` for netcode, `SerializableRun.Anonymized`.
- Public API on `Patches/Saves/ExtendedSaveTypes.cs`: `ExtendedSaveTypes.RegisterSavedValue<TargetType, T>(string id, Func<TargetType,T?> getter, Action<TargetType,T?> setter, Action<T,PacketWriter> serializer, Func<PacketReader,T> deserializer)`. With `TargetType = IRunState` it writes to `SerializableRun`. The `ExtendedSaveHandlers<IRunState,SerializableRun>.RegisterSave<T>(...)` overloads are public too, and `T : IPacketSerializable, new()` needs no serializer args. Must be called in the mod initializer (before BaseLib builds the properties; later registrations log a warning and are not saved).
- Simpler per-instance API: `Utils/SpireField.cs` `SavedSpireField<TKey,TVal>(Func<TKey,TVal?> defaultVal, string name)` (`TKey : class`). For `TKey` = `CardModel`, `RelicModel`, `EnchantmentModel` or `ModifierModel` with `TVal` = int, bool, string, enum, `ModelId`, `int[]`, `SerializableCard` it uses the base game's `SavedProperties`. For other combos, for example `TKey = IRunState`, it takes the custom path `RegisterCustomSave()` and needs `Serializer`/`Deserializer` set (builtin ones exist in `SavePatchUtils` for bool, byte, short, int, long, float, double, decimal, string, ModelId). The type must be saveable (`ExtendedSaveTypes.IsSaveTypeSupported`), else it throws and wants `RegisterAdditionalSaveType`.
- JSON it produces: a property `save_dict_<TypeName>` holding a `Dictionary<string,T>` keyed by the id you registered, for example `"save_dict_Int32": { "wayfinder_enemy_hp": 130 }` on `current_run.save`. Type names come from `MakeTypeName`. System types use `Type.Name`; others use the full name, hence the sample's `save_dict_List[BaseLib.Abstracts.CardModifier+ModifierSave]`.
- Storing floats: the `SavedProperties` path supports only int, bool, string, enum, ModelId, int[], SerializableCard types (`SavePatchUtils.IsStoreTypeBaseSupported`). The custom path supports float and double through PacketWriter. Using whole-number percents (int) keeps everything simple and matches slider values.
- Load flow: the mod writes its snapshot into the registered field in a new-run hook; on load `RunState.FromSerializable` postfix calls the setter, so the loaded `IRunState` has the same values. All consumers (enemy scaling, rewards, treasure) must then read from the run snapshot instead of the config statics. A run with no data (started before v0.2.0, or Wayfinder off) returns the default, so treat missing as Off/vanilla.

### History `.run` file: not automatically; options

- `Runs/RunHistoryUtilities.CreateRunHistoryEntry(SerializableRun run, bool victory, bool isAbandoned, PlatformType)` builds a fresh `RunHistory` copying only a fixed list of fields (players with `Deck`, `Relics`, `Potions`; `MapPointHistory`; `Modifiers`; `Acts`; seed; times; ascension; game_mode). Run-level extended data on `SerializableRun` is NOT copied. `RunHistory` is a separate class with no extension slot, and BaseLib's `ExtendedSavePatches.AddContext` only patches the `SerializableRun`, `SerializableCard`, etc. `*PropInit` methods, not `RunHistoryPropInit`. So a run-level `SavedSpireField` reaches `current_run.save` but not the `.run`.
- What does reach the `.run`: the sample `1794162390.run` (build v0.107.1) has `save_dict_List[BaseLib.Abstracts.CardModifier+ModifierSave]` on every card in `players[].deck`, `map_point_history[][].player_stats[].card_choices[].card`, `cards_gained`, `cards_removed`, `cards_enchanted`. This is BaseLib's own card modifier data, proving that extended data attached to a `SerializableCard` rides along into history. Relics in the sample (`players[].relics[]`) have no `save_dict` only because nothing registered one; `SerializableRelic` has the same extension mechanism.
- Option A, card/relic carrier: attach a `SavedSpireField<RelicModel, int>` (or on the starter relic or a card) holding the settings. Works with no patching of history, but the data repeats and is awkward; the carrier must exist for the whole run and a run file reader would need to read it from a relic or card. A relic would show as a relic in the UI unless a hidden/non-listed one is used, so the carrier needs care.
- Option B, Modifier carrier: `RunHistory.Modifiers` copies `run.Modifiers` (`List<SerializableModifier>`, each `{ id, props: SavedProperties }`, in the `.run` as top-level `modifiers`, empty in the sample). A custom `ModifierModel` subclass with `[SavedProperty]` ints/bools/enums would be saved to both `current_run.save` and the `.run` natively. Costs: `ModifierModel`s are intended for daily and custom runs; `Neow.cs` branches on `RunState.Modifiers.Count <= 0` (lines 41, 217), `NTopBar.cs:322` shows a modifier container when any exist, and `SaveUtil.ModifierOrDeprecated` must resolve the id (needs ModelDb registration of a mod model). Likely changes Neow behavior, so risky; needs a playtest before committing.
- Option C, patch the history writer (recommended for the `.run`): add a Harmony postfix on `RunHistoryUtilities.CreateRunHistoryEntry` (or `SaveManager.SaveRunHistory(RunHistory)` / the `RunHistorySaveManager.SaveHistory`) and add an extension property to `RunHistory` the way BaseLib does for `SerializableRun`: patch `MegaCritSerializerContext.RunHistoryPropInit` (private method in the generated context; BaseLib patches the equivalents with `harmony.Patch(typeof(MegaCritSerializerContext).DeclaredMethod(name + "PropInit"), postfix: ...)` and appends `JsonMetadataServices.CreatePropertyInfo(options, SavePatchUtils.QuickProps<RunHistory, T>(name, getter, setter))`). `SavePatchUtils.QuickProps<ModifyingType, PropType>(propName, getter, setter)` is public and does the JSON property plumbing. Keep the settings in a `ConditionalWeakTable` keyed by `RunHistory` (BaseLib's `SpireField<RunHistory,T>`), filled in the postfix from the run's snapshot. This is how `wayfinder` fields could appear as, for example, `"wayfinder": { "preset": "PowerCurve", "enemy_hp_pct": 130, ... }` at the top level of the `.run`.
- Option D, sidecar: write `history/<start_time>.wayfinder.json` next to the `.run` from the same postfix. No serializer patching; the reader finds it by `start_time` (the `.run` filename is the start time, for example 1794162390). Does not affect the game's history screen or load. Check that the game's history listing (`RunHistorySaveManager.LoadAllRunHistoryNames`) ignores extra files before trusting this.
- Unknown JSON properties: extra top-level `.run` properties should load fine in the vanilla game if `JsonSerializationUtility.FromJson<T>` does not reject unknown members; the `save_dict_*` on cards in the sample proves vanilla-compatible extra properties on history entries are tolerated for at least those types. Verify for a top-level `RunHistory` property before relying on option C.

## Answers in brief

- Control types: bool tickbox, int/float/double slider (`[ConfigSlider]`), enum dropdown, string line edit, color picker, button methods; collapsible `[ConfigSection]`s; `[ConfigVisibleIf]`.
- Preset sets others, others switch to Custom: not built in, but possible via `ConfigChanged` + `ConfigReloaded()` on the config class, with a re-entrancy guard.
- Storage: `~/.local/share/SlayTheSpire2/mod_configs/sts2_wayfinder_mod.cfg`, JSON string-to-string, saved when the settings screen closes, on quit, or via `Save()`/`SaveDebounced()`.
- Read only at run start: BaseLib gives live statics; snapshot them in a run-start hook (`RunManager.SetUpNewSingleplayer`/`SetUpNewMultiplayer`) and read the snapshot, never the statics, afterward.
- Persist in run save: yes, register an `IRunState` saved value with `ExtendedSaveTypes.RegisterSavedValue<IRunState,T>` (or `SavedSpireField<IRunState,T>` with serializers); it lands in `current_run.save` as `save_dict_<Type>` and is restored by the `RunState.FromSerializable` postfix.
- Write to `.run`: not by default (history copies only a fixed field list). Cheapest reliable paths: Harmony postfix on `RunHistoryUtilities.CreateRunHistoryEntry` plus a `RunHistory` property added through `MegaCritSerializerContext.RunHistoryPropInit` (option C), or a sidecar file (option D). A `ModifierModel` carrier (option B) records natively but changes Neow and the top bar.
