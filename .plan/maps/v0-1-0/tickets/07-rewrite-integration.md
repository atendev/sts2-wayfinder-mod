---
type: grilling
blocked_by: [02]
undermined_by: []
assets: []
---

# Decide where the rewrite happens and how saves behave

## Question

At what point in the map lifecycle does the mod rewrite the map, and how does it stay correct across save and load without rewriting twice? What does the `EnableWayfinder` toggle do when flipped mid-run or between acts? What should happen to a run started before the mod was installed, or with the toggle off? Which RNG drives the shuffle so the same seed gives the same path?

Research constraints: `Hook.AfterMapGenerated` also fires on load, and the conversion must run after the early hooks where `GoldenCompass`, `SpoilsMap`, and `BigGameHunter` replace the map. Act 1 starts on a Monster point when the run did not start with Neow, which conflicts with the rule that each act starts at its Ancient. Decide whether the path keeps that vanilla start.
