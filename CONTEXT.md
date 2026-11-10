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
The strict line that replaces an act's branching map, holding every point the vanilla map generated. It always starts at the act's start point and ends at its boss, and the player never chooses a direction.
_Avoid_: Route, snake, combined path

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
One adjustable number in the difficulty settings, such as boss HP or the card reward chance.
_Avoid_: Knob, slider, option

**Preset**:
A named set of tuning values the mod ships, such as Off, Bosses only, or Power curve. Changing any tuning value by hand makes the run Custom.
_Avoid_: Mode, difficulty level

**Enemy scaling**:
Making enemies stronger than vanilla during a Wayfinder run, according to where the player is on the run.
_Avoid_: Scaling (alone), buffs

**Reward rate**:
How often a point pays out a card, relic, gold, or max HP compared with vanilla.
_Avoid_: Loot, drop rate
