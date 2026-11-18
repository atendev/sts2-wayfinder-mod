# Wayfinder

Wayfinder is a Slay the Spire 2 mod that replaces each act's branching map with one line the player walks from start to finish.

## Language

**Point**:
Any single stop on an act's map, of any type: combat, elite, unknown, shop, rest site, or treasure.
_Avoid_: Node, room, encounter

**Encounter**:
A combat, as the game itself uses the word. Only some points hold an encounter.
_Avoid_: Fight, point

**Weak encounter**:
An easier encounter the game places at the front of an act's encounter list, so the act opens gently.
_Avoid_: Easy encounter, easy pool

**Unified path**:
The strict line that replaces an act's branching map. On the Acts path length it holds the points the act's plan rolled, and on the Full map it holds every point the vanilla map generated. It always starts at the act's start point and ends at its boss, and the player never chooses a direction.
_Avoid_: Route, snake, combined path

**Act plan**:
One act's count range for every point type and its length, from which the run seed rolls that act's mix. Monsters fill what the length leaves. The vanilla map supplies only the start point and the boss. Each plan has an identity (Build the deck, The wilds, The gauntlet) used for tuning and the mod description, never shown in game.
_Avoid_: Act template, act preset

**Path length**:
The difficulty setting that picks how an act's points are chosen. Acts rolls each act's mix from its act plan. Full map keeps every point the vanilla map generated, as v0.1 did.
_Avoid_: Map size, act length (alone)

**Elite pair**:
Two Elites side by side, allowed only in Act 3. A rest site comes right before the pair, no third Elite joins it, and no other Elite sits within 4 points on either side.
_Avoid_: Double elite, elite chain

**Start point**:
The first point of an act. It is the act's Ancient, except in Act 1 of a run that did not start with Neow, where it is a combat.
_Avoid_: Row zero, first floor

**Wayfinder run**:
A run that had the mod enabled when it started. Only a Wayfinder run gets unified paths, and a run in progress never becomes one or stops being one.
_Avoid_: Modded run

**Ordering rules**:
The constraints that decide where each point type may sit on the unified path, such as keeping elites apart and not clustering treasure.
_Avoid_: Balance, scaling

**Hard rule**:
An ordering rule the unified path must meet unless the act's mix of points makes that impossible.
_Avoid_: Requirement, constraint

**Preference**:
An ordering rule the unified path aims for and gives up first when the mix of points doesn't allow it.
_Avoid_: Soft rule, guideline

**Break order**:
The fixed sequence in which ordering rules give way when an act's mix of points can't satisfy them all.
_Avoid_: Fallback, relaxation

**Path layout**:
How the points of a unified path are arranged on the map screen, independent of their order.
_Avoid_: Shape, map shape

**Switchback**:
The path layout that runs the unified path back and forth across the map in ranks that alternate direction, from the Ancient at the bottom center to the boss at the top center.
_Avoid_: Snake, zigzag

**Rank**:
One horizontal stretch of a switchback. A rank is a screen arrangement and is separate from the game's map rows, which still hold one point each.
_Avoid_: Row, line, tier

**Difficulty settings**:
The options that change how hard a Wayfinder run is, separate from the toggle that turns Wayfinder on. They are fixed when a run starts and stay fixed for that run.
_Avoid_: Balance, balance settings

**Tuning value**:
One option in the difficulty settings, such as Scale bosses, Card choices, or Chest odds. It is a toggle or a named choice. The numbers behind it are fixed in code.
_Avoid_: Knob, slider

**Preset**:
A named set of tuning values the mod ships, such as Off, Bosses only, or Power curve. Changing any tuning value by hand makes the run Custom.
_Avoid_: Mode, difficulty level

**Enemy scaling**:
Making enemies stronger than vanilla during a Wayfinder run, according to where the player is on the run.
_Avoid_: Scaling (alone), buffs

**Relic chest**:
A treasure point's chest that gives a relic and vanilla's gold.
_Avoid_: Normal chest, full chest

**Gold chest**:
A treasure point's chest that gives extra gold and no relic, rolled in place of a relic chest.
_Avoid_: Empty chest

**Reward rate**:
How often a point pays out a card, relic, gold, or max HP compared with vanilla.
_Avoid_: Loot, drop rate
