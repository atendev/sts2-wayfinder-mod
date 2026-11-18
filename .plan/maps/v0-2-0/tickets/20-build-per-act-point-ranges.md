---
type: task
blocked_by: [19]
undermined_by: []
assets: []
---

# Build per-act point ranges and the Path length setting

## Question

Build the per-act ranges from [Decide per-act point ranges](./19-per-act-point-ranges.md). `UnifiedActMap.MixOf` stops reading the vanilla map's counts and rolls each act's length and type counts from the run seed, with Monsters filling the rest. Act 1 includes Underdocks. The ordering rules gain the per-act placement flavor: no Elite in Act 1's first third and Act 1 Elites at least 6 apart, Act 2 Unknown runs of 2 to 3 allowed, Act 3 Elite pairs (never three in a row, rest site right before the pair, no other Elite within 4 points either side). How does the pair rule fit the break order, and which existing rules become per act? Add the Path length setting (Acts, Full map) to the difficulty settings with Acts in every preset, snapshot it at run start, and record it in the sidecar. Size the normal encounter list to the new act lengths instead of doubling it. Put the act identities in the mod description. Record the change in `docs/changelog.md`.

## Handoff

Branch `t3/per-act-point-ranges-path-length-1` (branched from the first attempt's `7e072b8`). Last commit is the `wip:` commit holding the whole build. Audit round 0, no council yet. Scope: no Elite pick patch, no `PullNextEncounter`, no random normal picks; those belong to the sibling ticket "Build random encounter picks after each pool runs out" on `t3/random-encounter-picks`.

Done and passing (118 tests in `tests/OrderingTests`, mod build clean):
- `HardRuleSearch` takes `PlacementRules` (Act 1 distance 6 and first-third ban, Act 3 pairs with rest before, no third, clearance 4).
- `UnifiedActMap.From(source, seed, act, pathLength)`: Acts rolls `ActPlans.For(act-1).Roll`, Full map keeps `MixOf`.
- `DifficultyOptions.PathLength` (ctor default FullMap, Acts in all presets), `WayfinderConfig` section `MapPath`, `DifficultySettings.PathLengthFor`, localization keys, stale SCALE_PATH hover numbers fixed, sidecar records it through the JSON.
- `ExtendEncounterListPatch`: weak x2, list sized to `ActPlan.MaxFightPoints` on Acts, doubled otherwise.
- New `tests/OrderingTests/ActPlanTests.cs`, option and sidecar tests, README, mod description in `sts2_wayfinder_mod.json`, `CONTEXT.md` terms, `docs/changelog.md`.

Left, in order:
1. Run `dotnet build sts2_wayfinder_mod.csproj` once; it rewrites the BaseLib `min_version` in `sts2_wayfinder_mod.json` to 3.4.8. Revert that line to 3.4.7 before committing.
2. Council audit (`council-audit`) on the diff against `wayfinder/v0-2-0-master`, up to five rounds. Rounds used: 0.
3. Decision report to `.plan/maps/v0-2-0/assets/20-build-per-act-point-ranges-report.md`, unslop it, commit with the work. Delete this Handoff section in the resolution commit.
4. Push without force, open a PR against `wayfinder/v0-2-0-master`, link it, return PUSHED. Then CI result handling, timing close, resolution in the ticket file.

Known risks for the audit: `ScaleRoomCount` is not covered by a test (needs the game); hard-search pair state is tested through `ActPlanTests`; Off preset now uses Acts path length by decision.

Read first: this ticket, `ActPlans.cs`, `HardRuleSearch.cs` (`EliteAllowed`, `NextElite`), `ExtendEncounterListPatch.cs`, `ActPlanTests.cs`.
