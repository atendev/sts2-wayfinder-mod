---
type: task
blocked_by: [03, 07]
undermined_by: []
assets: []
---

# Build lower Elite relic rarity

## Question

Build the Lower Elite relic rarity toggle, using the hook from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md): swap in a `RelicReward` with an explicit rarity so an Elite's relic shifts toward Common while it still always drops one. Gated on `UnifiedActMap.IsLine` and the settings read at run start, per [the settings screen](./07-difficulty-settings-screen.md). How far does the shift go, and does it keep the seeded reward RNG counter aligned? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.
