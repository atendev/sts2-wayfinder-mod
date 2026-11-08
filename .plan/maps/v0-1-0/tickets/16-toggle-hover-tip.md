---
type: task
blocked_by: [07]
undermined_by: []
assets: []
---

# Say on the EnableWayfinder hover tip that it needs a new run

## Question

Add the hover tip text that [the rewrite decision](./07-rewrite-integration.md) requires, so the toggle says it only applies to a new run.

## Answer

The `.hover.desc` value in `settings_ui.json` now reads "Combine and reshuffle each act's points into a single unified path. Takes effect when you start a new run. A run in progress stays as it is." The toggle already has `[ConfigHoverTip]`, and the BaseLib 3.4.7 assembly contains the `.hover.desc` suffix, so no code changed.

The last sentence covers both directions of the toggle, because the rewrite reads it only on a new run's first map. The first sentence now says "points" instead of "encounters" to match `CONTEXT.md`.

Verified by parsing the JSON. The mod was not rebuilt, and the text has not been seen in the running game, so [the playthrough](./18-playthrough.md) has to confirm the tip shows it.

Excluded:

- A new localization key or attribute, since the existing key already feeds the tip.
- The manifest's "every encounter" wording, which belongs to [the reword ticket](./17-reword-readme-manifest.md).
