## Destination

Wayfinder v0.2.0 ships difficulty settings with the Off, Bosses only, and Power curve presets plus toggle and named-choice options. A Power curve run has been played and measured against the v0.1 baseline, with Act 3 Elites and bosses averaging 4 or more turns and 15 or more damage taken, and `docs/changelog.md` has a v0.2.0 entry.

## Notes

- This map overrides wayfinder's plan-only default, like the v0.1.0 map. It starts with decision tickets and moves on to build and playtest tickets once the decisions are in.
- Vocabulary lives in `CONTEXT.md`. This map added difficulty settings, tuning value, preset, enemy scaling, and reward rate. Don't call any of it "balance".
- The problem, measured in [the v0.1 baseline](./assets/00-v0-1-baseline.md): a Wayfinder run has more relics by the end of Act 1 than a whole vanilla run, and Act 3 fights end in under 2 turns for under 3 damage. Elites, Unknowns, and Treasure are the big relic sources. Monster fights paid out 87 to 147 max HP through per-fight effects.
- Settled while charting:
  - ~~Every point the vanilla map generated stays on the path.~~ Reversed by [per-act point ranges](./tickets/19-per-act-point-ranges.md): each act rolls its own length and type counts. Points are still never converted from one type into another.
  - Difficulty settings are a separate settings group with presets and per-value tuning. They are read when a run starts and stay fixed for that run, like EnableWayfinder.
  - Presets: **Off** keeps vanilla numbers, the same as v0.1. **Bosses only** gives bosses enemy scaling and changes nothing else. **Power curve** lowers reward rates and raises enemy scaling through Acts 2 and 3, so Act 3 is the hardest stretch. Changing any tuning value by hand makes the run **Custom**.
  - Power curve is the default preset when Wayfinder is on.
  - Enemy scaling raises HP and damage, driven by position in the run (act and point on the path), never by the player's power.
  - Card rewards in Power curve: normal combats, including combats from Unknowns, alternate between reward and no reward, starting with a reward. Elites and bosses always give one. Every card reward shows 4 choices. The alternation and the choice count are tuning values, and Bosses only keeps vanilla's reward every fight with 3 choices.
  - Treasure chests in Power curve roll an outcome table, such as a relic with a small amount of gold or a large amount of gold with no relic, instead of always giving a relic.
  - Elites always drop a relic. A "Lower Elite relic rarity" toggle shifts that relic toward Common. It is on in Power curve.
  - Event relics, individual relic effects, and gold rates stay vanilla. Gold is still a tuning value, defaulting to 100%.
  - Max HP inflation is handled by enemy damage, not by its own cut.
  - Testing: play a build, read its run file with the run file reader, and compare with the baseline. Each run should record which preset and tuning values it used.
- Game facts come from `references/decompiled/` (gitignored), generated from the installed `sts2.dll`. One baseline run is on v0.111.0 public beta, so check the decompile against the installed build before relying on a signature.
- Record each mod change in `docs/changelog.md` as build tickets land.

## Decisions so far

- [Commit the run file reader and the v0.1 baseline](./tickets/01-run-file-reader.md): `scripts/run_report.py` reproduces the baseline, labels runs Wayfinder, Vanilla, or Modded, counts shop and event relics through picked relic choices, and reads difficulty settings from a `wayfinder` key or a sidecar.
- [Find how enemy HP and damage are set and where scaling can hook in](./tickets/02-enemy-stats-and-scaling-hooks.md): ascension is hard-coded per monster and can't be reused, so HP scales through a `CombatState.CreateCreature` postfix and damage through a `ModifyDamageMultiplicative` listener that intents already show.
- [Find how card rewards, treasure chests, and Elite relics are built](./tickets/03-card-rewards-treasure-elite-relics.md): the 3 card choices are a literal in `RewardsSet.GenerateRewardsFor`, chest relic and gold come from two seeded calls with an empty chest already supported, and Elite rarity shifts by swapping in a `RelicReward` with an explicit rarity.
- [Find how BaseLib mod config can show presets and tuning values](./tickets/04-baselib-config-and-run-record.md): BaseLib has sliders, dropdowns, toggles, and sections, presets work through `ConfigChanged`, settings ride in the run save, and the `.run` history needs a patch or sidecar to record them.
- [Decide the enemy scaling curve for Power curve and Bosses only](./tickets/05-enemy-scaling-curve.md): a smooth path curve for Monster, Elite, and Unknown fights from 100% at Act 2's start to 180%/290% HP and 150%/260% damage at the ends of Acts 2 and 3, with boss values per act and 8 tuning values in all.
- [Decide the treasure outcome table](./tickets/06-treasure-outcome-table.md) — a chest is a relic chest or a gold chest at 2x vanilla gold, with a relic chance that starts at 20% per act, rises 20% per miss, resets on a relic, and forces a relic on the act's last chest; 3 tuning values.
- [Decide the difficulty settings screen](./tickets/07-difficulty-settings-screen.md) — a preset dropdown under Enable Wayfinder, then open sections of toggles and named choices (3 enemy scaling toggles, card rewards, chest odds, Elite rarity, gold rate), with a hand edit flipping the preset to Custom.
- [Decide how a run records its difficulty settings](./tickets/08-record-settings-per-run.md) — a snapshot in the run save at run start, and a `<start_time>.wayfinder.json` sidecar written next to the `.run` for Wayfinder runs only; no patch to the history format and nothing shown to the player.
- [Build enemy scaling for the path curve and bosses](./tickets/09-build-enemy-scaling.md) — `Difficulty/` holds the curve, three toggles, an HP postfix on `CreateCreature` for the encounter's own monsters, and a damage postfix on `Hook.ModifyDamage` that rounds per hit so the intent matches; toggles are hard-wired on until the settings screen lands.
- [Build the treasure outcome table](./tickets/10-build-treasure-outcome-table.md) — a rolled gold chest empties the relic through a `Hook.ShouldGenerateTreasure` flag and pays the multiplied gold in a `DoTreasureRoomRewards` prefix, with the miss count rebuilt from the map point history and no save data.
- [Build the difficulty settings screen](./tickets/11-build-difficulty-settings-screen.md) — the preset dropdown and option sections on `WayfinderConfig` with Custom detection, a run-start snapshot saved in the run save as JSON, and enemy scaling and chests reading it; the Custom banner became a hover tip and the in-game checks are still open.
- [Build alternating card rewards and the card choice count](./tickets/12-build-card-rewards.md) — a `GenerateRewardsFor` patch applies Card choices to combat rewards only and drops the card reward on every second normal combat, counted per act from `MapPointHistory`, unsubscribing the dropped reward's relic handler.
- [Build lower Elite relic rarity](./tickets/13-build-elite-relic-rarity.md) — a `GenerateRewardsFor` postfix swaps an Elite's relic reward for one with an explicit rarity rolled 75% Common, 20% Uncommon, 5% Rare from the same reward stream draw; Black Star, events, and shops stay vanilla.
- [Build the run record sidecar](./tickets/14-build-run-record-sidecar.md) — a `CreateRunHistoryEntry` postfix writes `history/wayfinder/<start_time>.json` for Wayfinder runs, in a subfolder because the game lists every file in `history/` as a run; the in-game confirmation is still open.
- [Find where combat, event, and chest gold is paid](./tickets/15-gold-rate-hooks.md) — every gold gain runs through `PlayerCmd.GainGold` and `Hook.ModifyGoldGained`, so one postfix there can apply the Gold rate; it stacks with the chest multiplier, shop prices don't scale, and the reward screen will show unscaled combat gold.
- [Build the Gold rate](./tickets/16-build-gold-rate.md) — a postfix on `Hook.ModifyGoldGained` scales every gold gain by 75%, 100%, or 125%, rounded per call and gated on the Wayfinder line; the unscaled reward-screen display is still open for a playtest.

- [Set the playtest run count, characters, and pass criteria](./tickets/17-playtest-plan.md) — one Ironclad Power curve run passes on Act 3 Elites and boss at 4+ turns and 15+ damage, 15 or fewer relics by the end of Act 1, under 100 max HP gained, a winnable run, and a gold display that doesn't read as a bug; tune and replay up to 3 misses, then ask.

- [Decide per-act point ranges and act identities](./tickets/19-per-act-point-ranges.md) — each act rolls its own length and type counts (about 96 points a run against 157), with Act 1 Build the deck, Act 2 The wilds, Act 3 The gauntlet; Elites fight each pool Elite once then pick at random, a Path length setting keeps Full map, and the path curve drops to 130%/150% HP and 130%/180% damage.
- [Build per-act point ranges and the Path length setting](./tickets/20-build-per-act-point-ranges.md) — each act rolls its length and type counts from `ActPlans` and the run seed with per-act Elite placement (Act 1 none in the first third and spaced out, Act 3 pairs); the Path length setting (Acts, Full map) is Acts in every preset, snapshotted and in the sidecar, and the normal list is sized to the act; `ScaleRoomCount` is untested in game.
- [Build random encounter picks after each pool runs out](./tickets/21-build-random-encounter-picks.md) — a `GenerateRooms` postfix rewrites each act's saved Elite and normal lists with the generation rng: Elites are a seeded shuffle of the pool then random picks skipping the last, normal fights are vanilla's list then random regular encounters, so picks survive a reload; untested in game.
- [Playtest a Power curve run and compare it with the baseline](./tickets/18-playtest-power-curve.md) — runs 4 and 5 on the shorter path both won and felt balanced: Act 3 Elites and boss 5.3 to 6.2 turns and 35 to 39 damage, 7 relics at the end of Act 1, and 55 and 69 max HP gained; the one miss (run 5 max HP) is accepted.

## Not yet specified

- Per-fight payouts, such as the 87 to 147 max HP gained from Monster fights. The playtest's max HP criterion tests whether enemy damage covers them; chart a ticket only if the run misses it.

## Out of scope

- Converting one point type into another, for example an Elite into a Monster.
- Changing individual relics, cards, or events.
- Tested co-op support.
