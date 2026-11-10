---
type: task
blocked_by: [06, 07]
undermined_by: []
assets: []
---

# Build the treasure outcome table

## Question

Build the chest table decided in [Decide the treasure outcome table](./06-treasure-outcome-table.md), using the hooks from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md). Roll the outcome in `TreasureRoomRelicSynchronizer.BeginRelicPicking` on the `TreasureRoomRelics` stream, and only inside the `Hook.ShouldGenerateTreasure` branch so Silver Crucible's chest gets no roll. Apply the gold chest multiplier in `OneOffSynchronizer.DoTreasureRoomRewards` while still drawing vanilla's `NextInt(42, 53)` so the `PlayerRng.Rewards` counter stays aligned. Gate everything on `UnifiedActMap.IsLine` and the settings read at run start. The settings are now a "Use treasure chest chances" toggle plus a Chest odds choice (Generous 25%/+25%/2x, Balanced 20%/+20%/2x, Stingy 15%/+15%/1.5x), per [the settings screen](./07-difficulty-settings-screen.md); with the toggle off, chests stay vanilla. How does the patch find the act's last treasure point and count misses since the last relic chest from the map point history, so a save and reload gives the same chance? Re-check the listed signatures against the installed build. Record the change in `docs/changelog.md`.
