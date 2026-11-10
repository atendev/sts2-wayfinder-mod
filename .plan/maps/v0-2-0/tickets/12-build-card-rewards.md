---
type: task
blocked_by: [03, 07]
undermined_by: []
assets: []
---

# Build alternating card rewards and the card choice count

## Question

Build the card rewards decided in the map Notes, using the hooks from [Find how card rewards, treasure chests, and Elite relics are built](./03-card-rewards-treasure-elite-relics.md): the literal 3 in `RewardsSet.GenerateRewardsFor` becomes the Card choices option, and normal combats, including combats from Unknowns, alternate between a card reward and none, starting with a reward, while Elites and bosses always give one. Both are gated on `UnifiedActMap.IsLine` and the settings read at run start, per [the settings screen](./07-difficulty-settings-screen.md). How does the alternation find its place from the map point history so a save and reload gives the same result? Re-check the signatures against the installed build. Record the change in `docs/changelog.md`.

## Answer

`Difficulty/CardRewardPatch.cs` patches the private `RewardsSet.GenerateRewardsFor(Player, AbstractRoom)`. Its prefix sets the run's Card choices value, and a prefix on the `CardReward(CardCreationOptions, int, Player, PlayerChoiceSynchronizer)` constructor applies it only while that method runs. That scope covers Monster, Elite, and boss rewards. Prayer Wheel builds through `CardCreationOptions.ForRoom` too, so the `CardCreationSource.Encounter` check would have caught it, and relic and event rewards keep 3. Both changes apply only on a `UnifiedActMap.IsLine` map and read `DifficultySettings.For(runState)`.

The alternation counts the run's earlier `MapPointHistory` entries that have a `RoomType.Monster` room, across all acts, and skips the current entry by reference. An even count gives a card reward. Unknown points that rolled a combat count, nothing new is saved, and a reload gives the same result. On a skipped combat the postfix removes the `CardReward` before `Populate`, so gold, potions, and the reward stream stay vanilla. It first unsubscribes the reward's `OnRelicObtained` handler from `player.RelicObtained`, because that handler throws before `Populate` and would crash the next relic pickup. Prayer Wheel's extra reward still pays out, since relic effects stay vanilla.

Signatures checked against `references/decompiled/sts2.txt`. Excluded: a per-act reset of the alternation, stripping Prayer Wheel's reward, and changing relic or event card counts. Still open: an in-game check of 4 cards on screen, and a playtest of a skipped combat followed by a relic pickup.
