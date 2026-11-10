---
type: task
blocked_by: [03, 07]
undermined_by: []
assets: []
---

# Build alternating card rewards and the card choice count

## Question

Build the card rewards decided in the map Notes, using the hooks from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md): the literal 3 in `RewardsSet.GenerateRewardsFor` becomes the Card choices option, and normal combats, including combats from Unknowns, alternate between a card reward and none, starting with a reward, while Elites and bosses always give one. Both are gated on `UnifiedActMap.IsLine` and the settings read at run start, per [the settings screen](./07-difficulty-settings-screen.md). How does the alternation find its place from the map point history so a save and reload gives the same result? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.
