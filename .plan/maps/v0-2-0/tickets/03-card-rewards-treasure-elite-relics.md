---
type: research
blocked_by: []
undermined_by: []
assets: [03-card-rewards-treasure-elite-relics.md]
---

# Find how card rewards, treasure chests, and Elite relics are built

## Question

How does the game build combat rewards? Where is the number of card choices set, and can a mod show 4 instead of 3? How can a mod skip the card reward on some normal combats, including combats that come out of Unknown points, while keeping gold and potions? How is a treasure chest's content decided, including its relic and gold, and how could a mod replace it with a seeded roll from an outcome table? How is an Elite's relic rarity rolled, and how could a mod shift it toward Common? Which of these already have seeded RNG streams a mod should reuse so runs stay deterministic? Do any relics or cards change the card choice count or chest contents, and how would they combine with the mod's changes?

## Answer

The game builds combat rewards in the private `RewardsSet.GenerateRewardsFor`, where the card count is a literal 3 passed to the `CardReward` constructor, so a Harmony prefix on that constructor can make it 4 (scope it by `CardCreationSource.Encounter` if events and relics should stay at 3). Removing the `CardReward` from the result of `GenerateRewardsFor` on flagged Monster combats keeps gold and potions, and Unknown combats arrive as ordinary `RoomType.Monster` rooms, so the alternation can count Monster entries in `MapPointHistory` with no new save state (Prayer Wheel adds its own card reward later via `Hook.ModifyRewards`). Treasure is separate: the relic comes from `TreasureRoomRelicSynchronizer.BeginRelicPicking` using the `TreasureRoomRelics` stream, the gold from the private `OneOffSynchronizer.DoTreasureRoomRewards` using `PlayerRng.Rewards` (42 to 52), and the UI already handles an empty chest. Elite relic rarity is rolled by `RelicFactory.RollRarity(Player)` (50/33/17 Common/Uncommon/Rare), best shifted by swapping the Elite `RelicReward` for one with an explicit rarity so shops and events stay vanilla. Silver Crucible's `ShouldGenerateTreasure` must still be respected, and the v0.111 public beta is not decompiled, so re-check the listed signatures. Details and file paths: [assets/03-card-rewards-treasure-elite-relics.md](../assets/03-card-rewards-treasure-elite-relics.md).
