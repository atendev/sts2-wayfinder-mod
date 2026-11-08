---
type: grilling
blocked_by: [06]
undermined_by: []
assets: []
---

# Decide how the ordering engine gets tested without the game

## Question

The repo has no test project, and the mod references the game assembly. How do the ordering rules and break order from [the ordering decision](./06-ordering-rules.md) get tested without the game running? Decide where the engine lives so it has no game dependency, which test runner to use, and which checks are required, such as determinism from a seed and every hard rule holding on batches of real mixes.

## Answer

The engine lives in the mod at `sts2_wayfinder_modCode/Ordering/` and uses only the base class library. A test project under `tests/` compiles those same files through linked `<Compile Include>` items and has no `sts2` reference, so a game type in that folder fails the test build. The mod's csproj gets `<Compile Remove="tests/**" />`, because its default globbing would otherwise compile the tests into the mod.

Randomness reaches the engine through a one-member interface, `int NextInt(int maxExclusive)`, and the engine builds its shuffles and jitter on top of that. The mod adapts the game's `Rng`, constructed as [the rewrite decision](./07-rewrite-integration.md) requires, and the tests adapt a small fixed generator written in the test project. Microsoft doesn't guarantee that a seeded `System.Random` keeps its sequence across major .NET versions, and the pinned paths below would change with it. The tests can't cover the game's generator, so the playthrough exercises the mod's adapter.

The tests use xUnit 2.9.x with plain loops over fixed seeds and no property library. Failure messages include the seed and the mix. The project targets `net9.0` with `<RollForward>Major</RollForward>`. That matches the `TargetFramework` in the mod's csproj and runs on the installed .NET 10 runtime. This machine has no .NET 9 runtime.

A mix holds only the points between the start point and the boss, as [the ordering decision](./06-ordering-rules.md) defines, so position 1 is the first point after the start point even when the start point is a Monster. The engine returns its path and the list of rules it broke. These checks are required:

1. The same mix and seed give the same path.
2. Every point in the mix lands on the path exactly once.
3. On vanilla mixes, no hard rule breaks across 500 seeds per case. That covers every hard rule in the ordering decision, including the cap of 3 Treasures in a row.
4. On `BigGameHunter`, elite-heavy, and single-type mixes, ordering never throws and breaks only rules in the break order up to the step the engine reached. Position 1 is a Monster and the last point is a Rest site on every mix that holds those types.
5. On 400 mixes of 1 to 7 points, drawn from seeds 1 to 400 by the test's fixed generator, a brute-force check of every permutation confirms two things. The step the engine reached is the lowest step any permutation can satisfy, and for each rule the engine left broken, no permutation satisfies that rule together with every rule the engine kept.
6. On vanilla mixes, at least 99% of paths keep the spread preferences and the cap of 4 Monsters and Elites in a row from [the preview](./08-preview-sample-paths.md). The preview measured 99.7% for that cap and 100.0%, rounded, for the other preferences. The engine's report says whether the spread was dropped, and the validator measures the run caps.
7. Three exact paths for fixed seeds stay pinned in one test. An in-process determinism check can't catch order that varies per process, such as `HashSet` iteration or `string.GetHashCode`, and co-op later depends on that not happening. An intended algorithm change updates the pinned paths.

Test mixes are the modeled per-act and per-ascension mixes from [the preview script](../assets/08-preview-sample-paths.py), plus hand-written edge mixes: empty, one type, no Monster, no Rest site, and under 6 points. If the playthrough finds a different mix, it becomes another case.

The test validator is written separately from the engine, from the text of [the ordering decision](./06-ordering-rules.md). The engine needs its own rule checks for the restore step, and sharing that code would let one misread rule pass both sides.

[Build the ordering engine](./11-build-ordering-engine.md) creates the test project and writes the engine test-first, so there is no separate setup ticket. The build ticket also adds `obj/` to `.gitignore`, because only the Godot build's output location keeps it out of git today.

Excluded:

- A separate class library for the engine. The mod would ship a second DLL, and the copy step and the loader would both need checking.
- Tests that reference the mod assembly, which would need `sts2.dll` and Godot at test time.
- An RNG interface mirroring the game's `Rng`, because the engine only needs `NextInt`.
- An engine that owns a seeded `System.Random`, which contradicts the rewrite decision.
- A `System.Random` adapter in the tests, for the sequence reason above.
- Targeting `net10.0`, which could let code that doesn't compile on .NET 9 pass.
- Installing the .NET 9 runtime, a system change that nothing here needs.
- A property-testing library such as FsCheck.
- Capturing real mixes from a game session, which would mean running the game before the engine exists.
- A GitHub Actions job, because the destination doesn't need it.
