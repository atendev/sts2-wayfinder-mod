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
