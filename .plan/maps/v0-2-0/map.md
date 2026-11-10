## Destination

Wayfinder v0.2.0 ships difficulty settings with the Off, Bosses only, and Power curve presets plus per-value tuning. A Power curve run has been played and measured against the v0.1 baseline, with Act 3 Elites and bosses averaging 4 or more turns and 15 or more damage taken, and `docs/changelog.md` has a v0.2.0 entry.

## Notes

- This map overrides wayfinder's plan-only default, like the v0.1.0 map. It starts with decision tickets and moves on to build and playtest tickets once the decisions are in.
- Vocabulary lives in `CONTEXT.md`. This map added difficulty settings, tuning value, preset, enemy scaling, and reward rate. Don't call any of it "balance".
- The problem, measured in [the v0.1 baseline](./assets/00-v0-1-baseline.md): a Wayfinder run has more relics by the end of Act 1 than a whole vanilla run, and Act 3 fights end in under 2 turns for under 3 damage. Elites, Unknowns, and Treasure are the big relic sources. Monster fights paid out 87 to 147 max HP through per-fight effects.
- Settled while charting:
  - Every point the vanilla map generated stays on the path. Points are never dropped or converted, for example an Elite into a Monster.
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

## Not yet specified

- The full list of tuning values and each preset's numbers. It comes together after the scaling curve, treasure table, and settings screen are decided.
- The build tickets for enemy scaling, card rewards, treasure, Elite relic rarity, and the settings screen. Their shape depends on the hooks the research tickets find.
- The playtest-and-tune loop: how many runs, which characters, and when the numbers count as good enough for v0.2.0.
- Per-fight payouts, such as the 87 to 147 max HP gained from Monster fights. Check whether enemy damage covers them or a playtest shows they still snowball.

## Out of scope

- Dropping points from the path or converting one point type into another. Every point the vanilla map generated stays, as it is.
- Changing individual relics, cards, or events.
- Tested co-op support.
