#!/usr/bin/env python3
"""Summarize Slay the Spire 2 `.run` history files as Markdown tables.

Usage:
    scripts/run_report.py [PATH ...] [--mode all|wayfinder|vanilla|modded] [--min-acts N]

Each PATH is a `.run` file or a history folder. With no PATH, every history
folder under the game's data directory is read. Output has a runs table, a
per-act table, a by-point-type table, and a difficulty settings table once a
run records them.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import sys
from collections import Counter
from pathlib import Path

VANILLA_MAX_ACT_POINTS = 18
FIGHT_TYPES = ("monster", "elite", "boss")
POINT_TYPE_ORDER = ("elite", "unknown", "treasure", "shop", "monster", "rest_site", "ancient", "boss")
POINT_TYPE_NAMES = {
    "elite": "Elite",
    "unknown": "Unknown",
    "treasure": "Treasure",
    "shop": "Shop",
    "monster": "Monster",
    "rest_site": "Rest site",
    "ancient": "Ancient",
    "boss": "Boss",
}


def data_roots() -> list[Path]:
    roots = []
    if os.environ.get("XDG_DATA_HOME"):
        roots.append(Path(os.environ["XDG_DATA_HOME"]) / "SlayTheSpire2")
    roots.append(Path.home() / ".local/share/SlayTheSpire2")
    if os.environ.get("APPDATA"):
        roots.append(Path(os.environ["APPDATA"]) / "SlayTheSpire2")
    roots.append(Path.home() / "Library/Application Support/SlayTheSpire2")
    return roots


def find_history_dirs(roots: list[Path] | None = None) -> list[Path]:
    found = []
    for root in roots if roots is not None else data_roots():
        for pattern in ("*/*/profile*/saves/history", "*/*/modded/profile*/saves/history"):
            found.extend(Path(p) for p in sorted(glob.glob(str(root / pattern))))
    return list(dict.fromkeys(found))


def is_modded_path(path: Path) -> bool:
    return "modded" in path.parts


def vanilla_twin(path: Path) -> Path | None:
    if not is_modded_path(path):
        return None
    parts = list(path.parts)
    parts.remove("modded")
    return Path(*parts)


def collect_run_files(paths: list[str]) -> list[Path]:
    targets = [Path(p) for p in paths] if paths else find_history_dirs()
    files = []
    for target in targets:
        if target.is_dir():
            files.extend(sorted(target.glob("*.run")))
        else:
            files.append(target)
    unique = list(dict.fromkeys(f.resolve() for f in files))
    present = set(unique)
    return [f for f in unique if not (is_modded_path(f) and vanilla_twin(f) in present)]


def load_settings(path: Path, run: dict) -> dict | None:
    if isinstance(run.get("wayfinder"), dict):
        return run["wayfinder"]
    sidecar = path.with_name(path.stem + ".wayfinder.json")
    if sidecar.is_file():
        with open(sidecar, encoding="utf-8") as fh:
            settings = json.load(fh)
        if not isinstance(settings, dict):
            raise ValueError(f"{sidecar.name} is not a JSON object")
        return settings
    return None


def classify(path: Path, run: dict, settings: dict | None) -> str:
    if settings is not None:
        return "Wayfinder"
    if any(len(act) > VANILLA_MAX_ACT_POINTS for act in run.get("map_point_history", [])):
        return "Wayfinder"
    if not is_modded_path(path):
        return "Vanilla"
    twin = vanilla_twin(path)
    if twin is not None and twin.is_file():
        return "Vanilla"
    return "Modded"


def pretty_id(model_id: str) -> str:
    return model_id.split(".", 1)[-1].replace("_", " ").title()


def player_stats(point: dict) -> dict:
    stats = point.get("player_stats") or [{}]
    return stats[0]


def picked_relics(stats: dict) -> list[str]:
    return [r.get("choice") for r in stats.get("relic_choices", []) if r.get("was_picked")]


def result_text(run: dict) -> str:
    acts = len(run.get("map_point_history", []))
    if run.get("win"):
        return "Win"
    if run.get("was_abandoned"):
        return f"Abandoned in Act {acts}"
    return f"Lost in Act {acts}"


def summarize(path: Path) -> dict:
    with open(path, encoding="utf-8") as fh:
        run = json.load(fh)
    settings = load_settings(path, run)
    player = run["players"][0]
    history = run.get("map_point_history", [])

    acts = []
    by_type: dict[str, Counter] = {}
    for index, act in enumerate(history):
        damage = {t: [] for t in FIGHT_TYPES}
        turns = {t: [] for t in FIGHT_TYPES}
        totals = Counter()
        hp_end = None
        for point in act:
            stats = player_stats(point)
            point_type = point.get("map_point_type", "?")
            for room in point.get("rooms", []):
                room_type = room.get("room_type")
                if room_type in damage:
                    damage[room_type].append(stats.get("damage_taken", 0))
                    turns[room_type].append(room.get("turns_taken", 0))
            cards = len(stats.get("cards_gained", []))
            relics = len(picked_relics(stats))
            totals["cards"] += cards
            totals["relics"] += relics
            totals["upgrades"] += len(stats.get("upgraded_cards", []))
            totals["gold"] += stats.get("gold_gained", 0)
            totals["max_hp"] += stats.get("max_hp_gained", 0)
            hp_end = (stats.get("current_hp"), stats.get("max_hp"))

            row = by_type.setdefault(point_type, Counter())
            row["points"] += 1
            row["relics"] += relics
            row["max_hp"] += stats.get("max_hp_gained", 0)
            row["cards"] += cards
        acts.append({
            "act": index + 1,
            "points": len(act),
            "damage": damage,
            "turns": turns,
            "totals": totals,
            "hp_end": hp_end,
            "final_act": index == len(history) - 1,
        })

    gained = Counter()
    for act in history:
        for point in act:
            stats = player_stats(point)
            gained.update(picked_relics(stats))
            gained.subtract(stats.get("relics_removed", []))
    starting = sum(1 for r in player.get("relics", []) if r.get("floor_added_to_deck", 1) <= 1 and gained[r.get("id")] <= 0)

    return {
        "id": path.stem,
        "path": path,
        "mode": classify(path, run, settings),
        "character": pretty_id(player.get("character", "?")),
        "ascension": run.get("ascension", 0),
        "build": run.get("build_id", "?"),
        "result": result_text(run),
        "won": bool(run.get("win")),
        "deck": len(player.get("deck", [])),
        "relics": len(player.get("relics", [])),
        "relics_accounted": starting + sum(v for v in gained.values() if v > 0),
        "acts": acts,
        "by_type": by_type,
        "settings": settings,
    }


def fight_cell(damage: list[int], turns: list[int]) -> str:
    if not damage:
        return "0"
    return f"{len(damage)}, {sum(damage) / len(damage):.1f} dmg, {sum(turns) / len(turns):.1f} t"


def boss_cell(act: dict, won: bool) -> str:
    damage, turns = act["damage"]["boss"], act["turns"]["boss"]
    if len(damage) == 1:
        return f"{damage[0]} dmg, {turns[0]} t"
    if damage:
        return fight_cell(damage, turns)
    return "died" if act["final_act"] and not won else "-"


def table(headers: list[str], rows: list[list[object]]) -> str:
    lines = ["| " + " | ".join(headers) + " |", "| " + " | ".join("---" for _ in headers) + " |"]
    lines.extend("| " + " | ".join(str(c) for c in row) + " |" for row in rows)
    return "\n".join(lines)


def settings_text(settings: dict) -> tuple[str, str]:
    preset = str(settings.get("preset", "-"))
    rest = ", ".join(f"{k}={v}" for k, v in settings.items() if k != "preset")
    return preset, rest or "-"


def render(runs: list[dict]) -> str:
    out = ["## Runs", ""]
    out.append(table(
        ["Run", "Mode", "Character", "Ascension", "Build", "Result", "Final deck", "Final relics"],
        [[r["id"], r["mode"], r["character"], r["ascension"], r["build"], r["result"], r["deck"], r["relics"]] for r in runs],
    ))

    out += ["", "## Per act", "", "Damage is the average damage taken per fight, and turns is the average turns per fight.", ""]
    rows = []
    for r in runs:
        for a in r["acts"]:
            t = a["totals"]
            hp = a["hp_end"]
            rows.append([
                r["id"], a["act"], a["points"],
                fight_cell(a["damage"]["monster"], a["turns"]["monster"]),
                fight_cell(a["damage"]["elite"], a["turns"]["elite"]),
                boss_cell(a, r["won"]),
                t["cards"], t["relics"], t["upgrades"], t["gold"], t["max_hp"],
                f"{hp[0]}/{hp[1]}" if hp else "-",
            ])
    out.append(table(
        ["Run", "Act", "Points", "Monster fights", "Elite fights", "Boss", "Cards", "Relics", "Upgrades", "Gold", "Max HP gained", "HP at end"],
        rows,
    ))

    out += ["", "## By point type, whole run", "", "Relics count every relic taken at the point, including shop and event relics. Cards count every card gained.", ""]
    rows = []
    for r in runs:
        known = [t for t in POINT_TYPE_ORDER if t in r["by_type"]]
        extra = sorted(t for t in r["by_type"] if t not in POINT_TYPE_ORDER)
        for point_type in known + extra:
            c = r["by_type"][point_type]
            rows.append([r["id"], POINT_TYPE_NAMES.get(point_type, point_type), c["points"], c["relics"], c["max_hp"], c["cards"]])
    out.append(table(["Run", "Source", "Points", "Relics", "Max HP", "Cards gained"], rows))

    mismatched = [r for r in runs if r["relics_accounted"] != r["relics"]]
    if mismatched:
        out += ["", "## Relic check", "", "Starting relics plus relics taken minus relics removed does not match the final relic count for these runs.", ""]
        out.append(table(["Run", "Accounted", "Final relics"], [[r["id"], r["relics_accounted"], r["relics"]] for r in mismatched]))

    recorded = [r for r in runs if r["settings"] is not None]
    if recorded:
        out += ["", "## Difficulty settings", ""]
        out.append(table(["Run", "Preset", "Tuning values"], [[r["id"], *settings_text(r["settings"])] for r in recorded]))

    return "\n".join(out) + "\n"


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("paths", nargs="*", help=".run files or history folders")
    parser.add_argument("--mode", choices=("all", "wayfinder", "vanilla", "modded"), default="all")
    parser.add_argument("--min-acts", type=int, default=0, help="skip runs that ended before this act")
    args = parser.parse_args(argv)

    files = collect_run_files(args.paths)
    if not files:
        print("No .run files found.", file=sys.stderr)
        return 1

    runs = []
    for f in files:
        try:
            summary = summarize(f)
        except (OSError, ValueError, KeyError, IndexError, TypeError) as exc:
            print(f"{f}: skipped, {exc}", file=sys.stderr)
            continue
        if args.mode != "all" and summary["mode"].lower() != args.mode:
            continue
        if len(summary["acts"]) < args.min_acts:
            continue
        runs.append(summary)

    if not runs:
        print("No runs matched.", file=sys.stderr)
        return 1
    sys.stdout.write(render(runs))
    return 0


if __name__ == "__main__":
    sys.exit(main())
