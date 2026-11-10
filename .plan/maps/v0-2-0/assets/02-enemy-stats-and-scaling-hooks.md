# Enemy HP, damage, and where scaling can hook in

Source: `references/decompiled-v0.107.1/` (paths below are relative to it). The mod's own code is in `sts2_wayfinder_modCode/`. Version note at the end.

## 1. How a monster gets its max HP

- Each monster class overrides `MonsterModel.MinInitialHp` and `MaxInitialHp` (`MegaCrit.Sts2.Core.Models/MonsterModel.cs`, abstract at lines 55 and 57). Many are fixed numbers, many pick between two numbers with ascension, for example `Mawler.MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 76, 72)` (`MegaCrit.Sts2.Core.Models.Monsters/Mawler.cs`).
- `CombatState.CreateCreature(MonsterModel, CombatSide, string slot)` (`MegaCrit.Sts2.Core.Combat/CombatState.cs` line 232) builds the `Creature`. For the enemy side it calls, in order:
  1. `Creature.SetUniqueMonsterHpValue(creaturesOnSide, RunState.Rng.Niche)` (`MegaCrit.Sts2.Core.Entities.Creatures/Creature.cs` line 369). It rolls an int in `[MinInitialHp, MaxInitialHp]`, avoiding values already used by siblings, and sets `_maxHp`, `_currentHp`, and `MonsterMaxHpBeforeModification`.
  2. `Creature.ScaleMonsterHpForMultiplayer(Encounter, Players.Count, RunState.CurrentActIndex)` (line 385). With 1 player it does nothing. With more it calls `SetMaxHpInternal(ScaleHpForMultiplayer(...))` then `SetCurrentHpInternal(MaxHp)`.
- `Creature.SetMaxHpInternal(decimal)` (line 493) clamps current HP to max. It does not raise current HP, so a scaling patch must call `SetCurrentHpInternal(MaxHp)` too, as the multiplayer path does. `CreatureCmd.SetMaxHp` is the async, event-raising version but is meant for in-combat changes.
- Callers of `CreateCreature` for enemies: `CombatRoom.StartCombat` (`MegaCrit.Sts2.Core.Rooms/CombatRoom.cs` line 209, once per `Encounter.MonstersWithSlots`), `CreatureCmd.Add<T>` / `Add(monster, ...)` (`MegaCrit.Sts2.Core.Commands/CreatureCmd.cs` lines 40 and 48, used for mid-combat spawns such as summons and splits), and `EventModel` (line 400, event combat layout). A patch on `CreateCreature` or `ScaleMonsterHpForMultiplayer` therefore also hits mid-combat spawns. Distinguish them by `CombatManager.Instance` state or by checking whether the encounter's `MonstersWithSlots` contains the monster.
- Room order inside `StartCombat`: `Encounter.GenerateMonstersWithSlots`, then `CreateCreature` + `AddCreature` per monster (`AddCreature` calls `Monster.SetUpForCombat()` which builds the move state machine, `CombatManager.cs` line 844), then `NCombatRoom.Create`, `CombatManager.SetUpCombat`, `Hook.AfterRoomEntered`, `AfterCombatRoomLoaded`. HP set in `CreateCreature` therefore lands before the health bar is built, so the UI shows the scaled number with no refresh.

## 2. How ascension raises HP and damage today

- `AscensionLevel` (`MegaCrit.Sts2.Core.Entities.Ascension/AscensionLevel.cs`): None, SwarmingElites, WearyTraveler, Poverty, TightBelt, AscendersBane, Inflation, Scarcity, **ToughEnemies** (HP), **DeadlyEnemies** (damage), DoubleBoss.
- There is no central multiplier. Every monster class hard-codes its own pair of numbers through `AscensionHelper.GetValueIfAscension(level, ascensionValue, fallbackValue)` (`MegaCrit.Sts2.Core.Helpers/AscensionHelper.cs`), which reads `RunManager.Instance.HasAscension(level)` (`MegaCrit.Sts2.Core.Runs/RunManager.cs` line 1601). Examples: `Axebot` and `Mawler` damage and HP properties. `grep -rl "AscensionLevel.ToughEnemies"` finds only monster classes plus the ascension UI.
- Consequence: ascension cannot be reused as a smooth multiplier. Turning on ToughEnemies and DeadlyEnemies for a Wayfinder run would give the fixed ascension numbers everywhere, with no act or position dependence, and would also bring the other ascension levels' ladder semantics. It is not a fit for per-point scaling.

## 3. How attack damage reaches the player

- A monster move stores `new SingleAttackIntent(damage)` or `new MultiAttackIntent(damage, repeats)` with a base damage int (`MegaCrit.Sts2.Core.MonsterMoves.Intents/`). The move body calls `DamageCmd.Attack(decimal)` (`MegaCrit.Sts2.Core.Commands/DamageCmd.cs` line 14).
- Actual damage is resolved in `CreatureCmd` (`CreatureCmd.cs` line 260): `Hook.ModifyDamage(runState, combatState, target, dealer, amount, props, cardSource, ModifyDamageHookType.All, CardPreviewMode.None, out modifiers)`.
- `Hook.ModifyDamage` (`MegaCrit.Sts2.Core.Hooks/Hook.cs` line 1486) calls `ModifyDamageInternal` (line 2505). Over `runState.IterateHookListeners(combatState)` it runs, in order: every listener's `ModifyDamageAdditive(target, amount, props, dealer, cardSource)`, then every `ModifyDamageMultiplicative(...)` (returns a factor), then `ModifyDamageCap`. Signatures are virtuals on `AbstractModel` (`MegaCrit.Sts2.Core.Models/AbstractModel.cs` lines 1552, 1565, 1580).
- `StrengthPower` is the template: additive, returns `Amount` only if `Owner == dealer` and `props.IsPoweredAttack()` (`MegaCrit.Sts2.Core.Models.Powers/StrengthPower.cs`). `IsPoweredAttack` is true for `ValueProp.Move` without `Unpowered`.
- Hook listeners (`RunState.IterateHookListeners`, `MegaCrit.Sts2.Core.Runs/RunState.cs` line 545; `CombatState.IterateHookListeners`, `CombatState.cs` line 410): deck cards, relics, potions, run `Modifiers`, `BadgeModels`, `MultiplayerScalingModel`, then mod subscribers, then combat state (creature powers, the `MonsterModel` itself, player combat piles). The monster model itself is a listener, so a patched or subclassed monster hook would also run.

## 4. Hooks that could scale HP and damage for one combat

### HP

No `Hook.ModifyMaxHp`-style hook exists for monster spawn. Options, best first:

1. **Harmony Postfix on `Creature.ScaleMonsterHpForMultiplayer(EncounterModel, int, int)`** (or on `CombatState.CreateCreature`). In the postfix: `if (Monster != null && IsEnemy)`, compute a factor, then `SetMaxHpInternal(MaxHp * factor)` and `SetCurrentHpInternal(MaxHp)`. It runs after the unique-HP roll and after the multiplayer scaling, so the two stack multiplicatively. `ScaleMonsterHpForMultiplayer` is a public instance method called once per enemy, and the early return for 1 player only skips its own body, not the postfix. Prefer `CreateCreature` Postfix if mid-combat spawns need separate handling, since it also gives `__result`.
2. Apply a power at combat start (see below). No HP effect, so not an option for HP.
3. Patch `MonsterModel.MinInitialHp` and `MaxInitialHp` getters. Rejected: abstract properties overridden in about 100 classes, so a patch needs one per class or a transpiler, and `Creature.MonsterMaxHpBeforeModification` would then include the scaling.

`Creature.MonsterMaxHpBeforeModification` (line 97) is set to the pre-scaling roll and is used by the game for display or history. Leave it unmodified unless a screen reads it.

### Damage

1. **Custom hook listener whose `ModifyDamageMultiplicative` returns a factor when `dealer` is an enemy and `props.IsPoweredAttack()`.** This is the cleanest: no per-monster edits, applies to every attack from every monster, and also reaches the intent display (section 5). Where to host it:
   - A power applied by the mod to each enemy creature at spawn, mirroring `Murderous` (`MegaCrit.Sts2.Core.Models.Modifiers/Murderous.cs`), which overrides `AfterCreatureAddedToCombat(Creature)` and calls `PowerCmd.Apply<StrengthPower>(...)` on non-player-side creatures, and `AfterRoomEntered` to apply to `combatRoom.CombatState.Creatures`. A power adds a visible icon and tooltip, needs localization and model registration, and has an `Amount` that could carry a percentage. Applied powers are saved with the creature state in a mid-combat save, which suits reload.
   - A model subscribed through `ModHelper.SubscribeForRunStateHooks(string id, RunHookSubscriptionDelegate del)` or `SubscribeForCombatStateHooks(string id, CombatHookSubscriptionDelegate del)` (`MegaCrit.Sts2.Core.Modding/ModHelper.cs` lines 100 and 120). The delegate returns `AbstractModel`s that join `IterateHookListeners`. No visible icon, no save impact, and the factor is computed live from run state. Needs a concrete `AbstractModel` subclass that the mod can instantiate; check whether a plain subclass works without `ModelDb` registration (not verified, and no reference mod in `references/` uses these subscribers, so this is untested).
2. Additive via `StrengthPower` on enemies. Works as an existing mechanism, but additive strength is the wrong shape for a percentage: a flat +N hurts 1-damage hits proportionally more than big hits, and it shows as a Strength buff icon. Also `Murderous` already does this at +3, so Wayfinder would collide with that modifier's look.
3. Harmony transpile or patch of `AttackIntent` damage constructors. Rejected: touches every monster.
4. `ModifyDamageCap` and `Hook.ModifyHpLost` are the wrong end (they cap or reduce player-side loss).

### Elite, normal, boss separately

- `CombatState.Encounter` (`ICombatState.Encounter`) is an `EncounterModel` with `RoomType` (`MegaCrit.Sts2.Core.Models/EncounterModel.cs` line 51), values `Monster`, `Elite`, `Boss` (`MegaCrit.Sts2.Core.Rooms/RoomType.cs`). `ScaleMonsterHpForMultiplayer` already receives the `EncounterModel`, and `MultiplayerScalingModel.GetMultiplayerScaling` is an in-game example of switching on `actIndex` and `encounter.RoomType == RoomType.Boss` (`MegaCrit.Sts2.Core.Models.Singleton/MultiplayerScalingModel.cs`).
- In a damage hook, the dealer is a `Creature`: `dealer.CombatState.Encounter.RoomType` and `dealer.CombatState.RunState`. In an HP postfix, `this.CombatState` (set by `AttachCreature`, `CombatState.cs` line 253) is available only after `CreateCreature` returns, so inside the `ScaleMonsterHpForMultiplayer` postfix use the `encounter` argument and fetch the run state via `RunManager.Instance.DebugOnlyGetState()` equivalent, or patch `CreateCreature` instead (it is an instance method on `CombatState`, so `__instance.RunState` and `__instance.Encounter` are directly available). **Recommendation: postfix `CombatState.CreateCreature`.**
- Unknown points that turn into fights are `RoomType.Monster`, so they scale as normal fights. If the plan wants them treated differently, read `runState.CurrentMapPoint.PointType` (`MapPointType.Unknown`).
- Double Boss (`AscensionLevel.DoubleBoss`) uses `ActMap.SecondBossMapPoint`; the mod's `UnifiedActMap` keeps it one row after the boss. Both are `RoomType.Boss`.

### Existing mechanisms that already do this

- **Ascension**: per-monster constants only (section 2). Not reusable as a multiplier.
- **Run modifiers** (`MegaCrit.Sts2.Core.Models.Modifiers/`: AllStar, BigGameHunter, CharacterCards, CursedRun, DeadlyEvents, Draft, Flight, Hoarder, Insanity, Midas, Murderous, NightTerrors, SealedDeck, Specialized, Terminal, Vintage): none scales enemy HP or damage by a factor. `Murderous` gives enemies +3 Strength flat. They are chosen in custom or daily runs and are saved on the run, so Wayfinder could not switch one on without touching run setup.
- **MultiplayerScalingModel**: scales enemy HP at spawn (`ScaleHpForMultiplayer`) and enemy block through `ModifyBlockMultiplicative`, but only for 2 or more players, and has no damage scaling. Its factors are `1.1`, `1.2`, `1.2`, boss in act 3 `1.3`, times player count. This is the only act and boss aware multiplier in the game and shows the intended shape of a scaling hook, but it cannot be reused for solo (`playerCount <= 1` returns early) without a patch, and the player count multiplication is built in.

## 5. Does scaled damage show in the intent?

Yes, if the scaling goes through `Hook.ModifyDamage`. `AttackIntent.GetSingleDamage(targets, owner)` (`MegaCrit.Sts2.Core.MonsterMoves.Intents/AttackIntent.cs` line 99) takes `DamageCalc()` and runs it through `Hook.ModifyDamage(me.RunState, me.Creature.CombatState, me.Creature, owner, DamageCalc(), ValueProp.Move, null, ModifyDamageHookType.All, CardPreviewMode.None, ...)`, where `me` is the local player (`LocalContext.GetMe`). The same value drives the intent number, the multi-hit total (`MultiAttackIntent.GetTotalDamage` is `GetSingleDamage * Repeats`), the texture and animation bucket (`<5`, `<10`, `<20`, `<40`, else) in `GetTexture` and `GetAnimation`, and the tooltip description. So a `ModifyDamageMultiplicative` or `Additive` listener that returns the same factor in preview and in the real hit keeps the intent correct, and a patch that only edits the base damage ints would not.

Caveats:
- The intent preview uses the local player as the target, and `CardPreviewMode.None`. The hook must not depend on `cardSource` and should tolerate `target` being the local player only.
- The result is truncated to int per hit (`(int)num`), then the real hit goes through the same `Hook.ModifyDamage` with a decimal, and `CreatureCmd` rounds when applying. If the factor yields fractions, preview and real damage can differ by 1 per hit. Round inside the hook (return a factor that gives an integer for the monster's base, or round in an additive hook) if exact agreement matters. Not verified in code which rounding `CreatureCmd` uses; check before building.
- Because `ModifyDamageMultiplicative` stacks after additive Strength and before Vulnerable-style multipliers depending on listener order, scaled enemies with Strength get more total damage. Listener order inside the multiplicative phase follows `IterateHookListeners` order (run-state models first, then mod subscribers, then combat state).

## 6. What a mod needs to know about position on the unified path

All of this is readable inside a combat with no new state:

- Act: `IRunState.CurrentActIndex` (0 to 2; `MultiplayerScalingModel.GetMultiplayerScaling` throws on any other value, so the game assumes 3 acts). `IRunState` is in `MegaCrit.Sts2.Core.Runs/IRunState.cs` line 31.
- Position in the act: `IRunState.CurrentMapCoord` (`MapCoord?`, line 48, null before the first point) with `.row`, or `CurrentMapPoint`. In a Wayfinder map, `UnifiedActMap` places one point per row in column 3: row 0 is the start, rows 1..N are the ordered path, row N+1 is the boss, and row N+2 is the second boss if present (`sts2_wayfinder_modCode/UnifiedPath/UnifiedActMap.cs`). So `row` is the position, and the path length is `runState.Map.BossMapPoint.coord.row - 1`. Fraction through the act: `(row - 1) / (N - 1)`.
- Also available: `IRunState.ActFloor` (floor within the act, line 75) and `TotalFloor` (floors across the run, line 81). These count visited points from history and, on a straight path, track the row, but they also count rooms that are not on the path in some cases. Prefer the map coord.
- Is this a Wayfinder run: the mod's existing test is `UnifiedActMap.IsLine(runState.Map)` (true for a one-point-per-row center-column map with no start points), used by `Act3RelicCheckPatch.cs` and `RewriteMapPatch.cs`. `WayfinderConfig.EnableWayfinder` is only read at run start; `IsLine` is the reload-safe test because a saved run restores a `SavedActMap`. Check that `IsLine` also holds for a `SavedActMap` (its `startMapPoints` must be empty and every point in column 3); `RewriteMapPatch` already relies on this, so it is believed to hold.
- Point type: `CurrentMapPoint.PointType` (`MapPointType`: Monster, Elite, Unknown, Shop, RestSite, Treasure, Boss) and the room type from `CombatState.Encounter.RoomType`.
- Timing: `CurrentMapCoord` is already set when `CreateCreature` runs. `CreateCreature` itself reads `RunState.CurrentMapCoord` to seed the monster Rng (`CombatState.cs` line 242), which confirms it is populated by then.

## Recommendation summary

- HP: Harmony Postfix on `CombatState.CreateCreature`, enemy side only: `SetMaxHpInternal`, then `SetCurrentHpInternal(MaxHp)`, factor from act index, row fraction, and `Encounter.RoomType`.
- Damage: a hook listener implementing `ModifyDamageMultiplicative` (dealer is an enemy, `props.IsPoweredAttack()`), registered through `ModHelper.SubscribeForCombatStateHooks`, or a power applied in `AfterCreatureAddedToCombat` like `Murderous`. Both keep intents correct.
- Gate both on `UnifiedActMap.IsLine(runState.Map)` and the preset chosen at run start.

## Version sensitivity (decompile v0.107.1, one test run on v0.111.0)

The mod manifest says `min_game_version` 0.107.0. Signatures worth rechecking on 0.111.0 before building: `Creature.ScaleMonsterHpForMultiplayer`, `CombatState.CreateCreature`, `Hook.ModifyDamage` parameter list, `AbstractModel.ModifyDamageMultiplicative`, `ModHelper.SubscribeFor*Hooks`, and `AttackIntent.GetSingleDamage`. `references/decompiled/sts2.txt` is the installed-dll dump and shows the same call graph for `CreateCreature` and `ScaleMonsterHpForMultiplayer` (lines about 80707 and 84103), but its generation date (Nov 8) and game build were not confirmed, so treat it as the same version as the one the baseline run used only after checking the dll. The act-count assumption (3 acts) and the `MultiplayerScalingModel` factors are also data the game may rebalance.

## Not verified

- Whether a plain mod-defined `AbstractModel` subclass returned from `SubscribeForCombatStateHooks` needs `ModelDb` registration.
- How `CreatureCmd` rounds a fractional damage result versus the intent's `(int)` truncation.
- Whether scaling `CreateCreature` also catches monsters spawned mid-combat in a way that is wanted (summons, splits); decide per design.
