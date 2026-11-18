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

Branch `t3/per-act-point-ranges-path-length`. WIP commit holds the work so far (does not build: `HardRuleSearch.TryOrder` has no 4th arg yet). Audit round 0. Baseline: 90 tests pass in `tests/OrderingTests`. Curve drop (130/150 HP, 130/180 dmg) is already in `EnemyScalingCurve.cs`.

Done: `Ordering/PlacementRules.cs` (NoEliteInFirstThird, EliteDistance, ElitePairs, PairClearance 4, GapCap), `Ordering/ActPlans.cs` (3 plans, `Roll`, `MaxFightPoints`, `ActPlans.For(actIndex)`), `PathOrderer.Order(mix, rng, placement)`, `SpreadPlacer` `PlaceElites`/`PlaceSpaced`/`PlaceGroups` (pairs as R E E blocks).

Left, in order:
1. `HardRuleSearch`: take `PlacementRules`. Act 1: elite distance 6 under EliteGap (step 5), first-third elite ban under OpeningBan (step 6), `CannotFit` earliest/spacing. Act 3: adjacent Elite allowed under ElitesApart (step 7) only as a pair: rest directly before, no third. Pair clearance 4 either side under EliteGap. Extra state: since (cap GapCap), gapBefore, lastWasPair, eliteHadRestBefore; zero for Standard, key as record struct. Relax pruning when pairs allowed.
2. `UnifiedActMap.From(source, seed, act, pathLength)`: Acts rolls `ActPlans.For(act-1).Roll(rng)` then orders; FullMap keeps `MixOf`, Standard rules, identical RNG use.
3. `DifficultyOptions.PathLength` (Acts, FullMap; ctor default FullMap; Acts in all presets), `WayfinderConfig` section `MapPath`, localization keys (`PATH_LENGTH.*`, `MAP_PATH.title`), fix stale SCALE_PATH hover text (+30%/+50% HP, +30%/+80% dmg). `DifficultySettings.PathLengthFor` returns FullMap when no snapshot. `RewriteMapPatch` passes it.
4. `ExtendEncounterListPatch`: Acts mode keeps weak x2, sizes regulars to `MaxFightPoints` minus weak, via `ldarg.0` + act index from `RunManager.Instance.DebugOnlyGetState().Acts`; fallback doubling. New `PullNextEncounter` postfix: Elites first pool-size visits in list order, then seeded random never repeating the last; normal overflow random regular. Pure `EncounterPicks` plus tests.
5. Tests (new `ActPlanTests`, validators per act), README and mod description act identities, `docs/changelog.md`, CONTEXT.md terms, `dotnet test`, build check.

Council: none yet. Read first: this ticket, ticket 19 Answer, `HardRuleSearch.cs`, `ActPlans.cs`.
