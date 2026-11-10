import importlib.util
import io
import json
from contextlib import redirect_stderr, redirect_stdout
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "run_report.py"
spec = importlib.util.spec_from_file_location("run_report", SCRIPT)
run_report = importlib.util.module_from_spec(spec)
spec.loader.exec_module(run_report)


def point(point_type, room_type=None, damage=0, turns=0, relics=(), bought=(), removed=(), cards=0, max_hp=0, hp=(50, 80)):
    stats = {
        "damage_taken": damage,
        "current_hp": hp[0],
        "max_hp": hp[1],
        "max_hp_gained": max_hp,
        "gold_gained": 10,
        "cards_gained": [{"id": f"CARD.C{i}"} for i in range(cards)],
        "relic_choices": [{"choice": r, "was_picked": True} for r in relics] + [{"choice": "RELIC.SKIPPED", "was_picked": False}],
    }
    if bought:
        stats["bought_relics"] = list(bought)
    if removed:
        stats["relics_removed"] = list(removed)
    rooms = [{"room_type": room_type, "turns_taken": turns}] if room_type else []
    return {"map_point_type": point_type, "player_stats": [stats], "rooms": rooms}


def run(acts, relics, win=True, **extra):
    data = {
        "ascension": 0,
        "build_id": "v0.111.0",
        "win": win,
        "was_abandoned": False,
        "map_point_history": acts,
        "players": [{"character": "CHARACTER.IRONCLAD", "deck": [{}] * 10, "relics": relics}],
    }
    data.update(extra)
    return data


STARTER = {"id": "RELIC.BURNING_BLOOD", "floor_added_to_deck": 1}


class RunReportTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def write(self, rel, data):
        path = self.root / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(data))
        return path

    def basic_run(self, **extra):
        acts = [[
            point("monster", "monster", damage=4, turns=3, cards=1, max_hp=2),
            point("shop", "shop", relics=["RELIC.LAMP"], bought=["RELIC.LAMP"], cards=2),
            point("unknown", "event", relics=["RELIC.FLOWER"]),
            point("elite", "elite", damage=10, turns=5, relics=["RELIC.ANCHOR"], removed=["RELIC.FLOWER"]),
            point("boss", "boss", damage=20, turns=6, hp=(30, 82)),
        ]]
        relics = [STARTER, {"id": "RELIC.LAMP", "floor_added_to_deck": 2}, {"id": "RELIC.ANCHOR", "floor_added_to_deck": 4}]
        return run(acts, relics, **extra)

    def test_counts_picked_relics_once_including_shop_and_event(self):
        summary = run_report.summarize(self.write("profile1/saves/history/1.run", self.basic_run()))
        act = summary["acts"][0]
        self.assertEqual(act["totals"]["relics"], 3)
        self.assertEqual(summary["by_type"]["shop"]["relics"], 1)
        self.assertEqual(summary["by_type"]["unknown"]["relics"], 1)
        self.assertEqual(summary["relics_accounted"], summary["relics"])

    def test_relic_check_flags_unrecorded_relics(self):
        data = self.basic_run()
        data["players"][0]["relics"].append({"id": "RELIC.GHOST", "floor_added_to_deck": 3})
        summary = run_report.summarize(self.write("profile1/saves/history/1.run", data))
        self.assertIn("## Relic check", run_report.render([summary]))

    def test_per_act_row_matches_baseline_format(self):
        summary = run_report.summarize(self.write("profile1/saves/history/1.run", self.basic_run()))
        out = run_report.render([summary])
        self.assertIn("| 1 | 1 | 5 | 1, 4.0 dmg, 3.0 t | 1, 10.0 dmg, 5.0 t | 20 dmg, 6 t | 3 | 3 | 0 | 50 | 2 | 30/82 |", out)

    def test_boss_cell_says_died_when_lost_before_boss(self):
        data = self.basic_run(win=False)
        data["map_point_history"][0].pop()
        summary = run_report.summarize(self.write("profile1/saves/history/1.run", data))
        self.assertEqual(run_report.boss_cell(summary["acts"][0], summary["won"]), "died")
        self.assertEqual(summary["result"], "Lost in Act 1")

    def test_long_act_is_wayfinder(self):
        data = self.basic_run()
        data["map_point_history"][0] *= 5
        summary = run_report.summarize(self.write("modded/profile1/saves/history/1.run", data))
        self.assertEqual(summary["mode"], "Wayfinder")

    def test_modded_copy_of_vanilla_run_is_vanilla(self):
        self.write("profile1/saves/history/1.run", self.basic_run())
        summary = run_report.summarize(self.write("modded/profile1/saves/history/1.run", self.basic_run()))
        self.assertEqual(summary["mode"], "Vanilla")

    def test_short_modded_run_without_settings_is_modded(self):
        summary = run_report.summarize(self.write("modded/profile1/saves/history/2.run", self.basic_run()))
        self.assertEqual(summary["mode"], "Modded")

    def test_reads_settings_from_run_file(self):
        data = self.basic_run(wayfinder={"preset": "PowerCurve", "enemy_hp_pct": 130})
        summary = run_report.summarize(self.write("modded/profile1/saves/history/3.run", data))
        self.assertEqual(summary["mode"], "Wayfinder")
        self.assertIn("| 3 | PowerCurve | enemy_hp_pct=130 |", run_report.render([summary]))

    def test_reads_settings_from_sidecar(self):
        path = self.write("modded/profile1/saves/history/4.run", self.basic_run())
        self.write("modded/profile1/saves/history/4.wayfinder.json", {"preset": "BossesOnly"})
        summary = run_report.summarize(path)
        self.assertEqual(summary["settings"], {"preset": "BossesOnly"})

    def test_no_settings_section_without_settings(self):
        summary = run_report.summarize(self.write("profile1/saves/history/1.run", self.basic_run()))
        self.assertNotIn("## Difficulty settings", run_report.render([summary]))

    def test_folder_scan_prefers_vanilla_copy_and_skips_backups(self):
        vanilla = self.write("profile1/saves/history/1.run", self.basic_run())
        self.write("modded/profile1/saves/history/1.run", self.basic_run())
        self.write("modded/profile1/saves/history/1.run.backup", self.basic_run())
        only_modded = self.write("modded/profile1/saves/history/2.run", self.basic_run())
        files = run_report.collect_run_files([str(self.root / "modded/profile1/saves/history"), str(self.root / "profile1/saves/history")])
        self.assertEqual(sorted(files), sorted([vanilla.resolve(), only_modded.resolve()]))

    def test_finds_history_dirs_under_steam_ids(self):
        self.write("steam/123/profile1/saves/history/1.run", self.basic_run())
        self.write("steam/123/modded/profile1/saves/history/2.run", self.basic_run())
        dirs = run_report.find_history_dirs([self.root])
        self.assertEqual(
            sorted(dirs),
            sorted([self.root / "steam/123/profile1/saves/history", self.root / "steam/123/modded/profile1/saves/history"]),
        )

    def test_folder_scan_keeps_same_start_time_in_other_profiles(self):
        first = self.write("steam/1/profile1/saves/history/1.run", self.basic_run())
        second = self.write("steam/2/profile1/saves/history/1.run", self.basic_run())
        files = run_report.collect_run_files([str(first.parent), str(second.parent)])
        self.assertEqual(sorted(files), sorted([first.resolve(), second.resolve()]))

    def test_sidecar_that_is_not_an_object_skips_the_run(self):
        self.write("modded/profile1/saves/history/5.run", self.basic_run())
        self.write("modded/profile1/saves/history/5.wayfinder.json", ["PowerCurve"])
        self.write("modded/profile1/saves/history/6.run", self.basic_run(wayfinder={"preset": "Off"}))
        out = io.StringIO()
        with redirect_stdout(out), redirect_stderr(io.StringIO()):
            rc = run_report.main([str(self.root / "modded/profile1/saves/history")])
        self.assertEqual(rc, 0)
        self.assertIn("| 6 | Off | - |", out.getvalue())
        self.assertNotIn("\n| 5 |", out.getvalue())

    def test_mode_filter(self):
        self.write("profile1/saves/history/1.run", self.basic_run())
        data = self.basic_run()
        data["map_point_history"][0] *= 5
        self.write("modded/profile1/saves/history/2.run", data)
        folders = [str(self.root / "profile1/saves/history"), str(self.root / "modded/profile1/saves/history")]
        for mode, kept, dropped in (("vanilla", "| 1 | Vanilla |", "\n| 2 |"), ("wayfinder", "| 2 | Wayfinder |", "\n| 1 |")):
            out = io.StringIO()
            with redirect_stdout(out):
                rc = run_report.main(folders + ["--mode", mode])
            self.assertEqual(rc, 0)
            self.assertIn(kept, out.getvalue())
            self.assertNotIn(dropped, out.getvalue())


if __name__ == "__main__":
    unittest.main()
