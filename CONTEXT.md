# Wayfinder

Wayfinder is a Slay the Spire 2 mod that replaces each act's branching map with one line the player walks from start to finish.

## Language

**Point**:
Any single stop on an act's map, of any type: combat, elite, unknown, shop, rest site, or treasure.
_Avoid_: Node, room, encounter

**Encounter**:
A combat, as the game itself uses the word. Only some points hold an encounter.
_Avoid_: Fight, point

**Unified path**:
The strict line that replaces an act's branching map, holding every point the vanilla map generated. It always starts at the act's Ancient and ends at its boss, and the player never chooses a direction.
_Avoid_: Route, snake, combined path

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
