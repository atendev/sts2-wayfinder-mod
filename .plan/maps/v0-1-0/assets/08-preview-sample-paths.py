#!/usr/bin/env python3
"""PROTOTYPE, throwaway. Previews unified paths per act using the ordering model from ticket 06.

Run: python3 -I 08-preview-sample-paths.py [paths_per_case]
Prints batch metrics and writes 08-preview-sample-paths.html next to this file.
Point counts are modeled from 01-vanilla-act-point-counts.md, not measured in game.
"""
import importlib.util
import json
import pathlib
import random
import sys

sys.dont_write_bytecode = True
HERE = pathlib.Path(__file__).resolve().parent
spec =importlib.util.spec_from_file_location("ordering", HERE / "06-ordering-stress-test.py")
st = importlib.util.module_from_spec(spec)
spec.loader.exec_module(st)
BASE_BROKEN = st.broken_rules

ACTS = {
    "Act 1 Overgrowth": dict(rooms=15, rests=(6, 7), unknowns=(10, 14)),
    "Act 2 Hive": dict(rooms=14, rests=(6, 7), unknowns=(9, 13)),
    "Act 3 Glory": dict(rooms=13, rests=(5, 6), unknowns=(9, 13)),
}
MODES = {
    "Ascension 0, 5 elites": dict(elites=5, hunter=False),
    "Ascension 1+, 8 elites": dict(elites=8, hunter=False),
    "BigGameHunter": dict(elites=5, hunter=True),
}
VARIANTS = {
    "Decided rules": (None, False),
    "Fight cap 5, strict": (5, False),
    "Fight cap 4, strict": (4, False),
    "Fight cap 5, gives way first": (5, True),
    "Fight cap 4, gives way first": (4, True),
}
SAMPLE_SIZE = 40
WORST_SIZE = 8


def act_mix(rng, act, elites, hunter):
    while True:
        rooms = act["rooms"]
        widths = [rng.choices(range(1, 8), [2, 8, 16, 24, 24, 16, 10])[0] for _ in range(rooms)]
        first, treasure, last = max(2, widths[0]), widths[rooms - 7], widths[-1]
        n = max(int(max(sum(widths), 1) * rng.uniform(0.8, 1.0)), first + treasure + last)
        rest = last + rng.randint(*act["rests"]) - (rng.randint(1, 2) if rng.random() < 0.2 else 0)
        elite = max(0, elites - (rng.random() < 0.2))
        shop = max(0, 3 - (rng.random() < 0.1))
        unknown = rng.randint(*act["unknowns"])
        monster = n - rest - elite - shop - unknown - treasure
        if monster >= first:
            if hunter:
                elite += monster - first
                monster = first
            return st.mix_of(E=elite, R=rest, S=shop, T=treasure, M=monster, U=unknown)


def set_fight_cap(cap):
    def broken(p, mix):
        out = BASE_BROKEN(p, mix)
        if cap and st.max_run(p, "ME") > cap:
            out = out | {"fight cap"}
        return out

    st.broken_rules = broken


def percentile(values, q):
    ordered = sorted(values)
    return ordered[min(len(ordered) - 1, int(len(ordered) * q))]


def window_max(p, kinds, size):
    return max(sum(c in kinds for c in p[i : i + size]) for i in range(max(1, len(p) - size + 1)))


def metrics(paths, levels, cap):
    rows = {
        "Points on the path": [len(p) for p in paths],
        "Longest stretch without a rest site": [st.max_run(p, "ESTMU") for p in paths],
        "Longest stretch without a shop": [st.max_run(p, "ERTMU") for p in paths],
        "Most fights in a row": [st.max_run(p, "ME") for p in paths],
        "Most fights in any 8 points": [window_max(p, "ME", 8) for p in paths],
        "Most elites in any 10 points": [window_max(p, "E", 10) for p in paths],
        "Position of the first elite": [p.index("E") + 1 for p in paths if "E" in p],
    }
    out = {k: dict(avg=round(sum(v) / len(v), 1), p95=percentile(v, 0.95), worst=max(v)) for k, v in rows.items() if v}
    return dict(
        table=out,
        adjacent_chests=round(sum(st.max_run(p, "T") >= 2 for p in paths) / len(paths), 3),
        full_rules=round(sum(lv == 0 for lv in levels) / len(levels), 3),
        cap_met=round(sum(st.max_run(p, "ME") <= cap for p in paths) / len(paths), 3) if cap else None,
    )


def batch(act, mode, cap, gives_way, count, seed):
    set_fight_cap(cap)
    rng = random.Random(seed)
    paths, levels = [], []
    for _ in range(count):
        mix = act_mix(rng, act, mode["elites"], mode["hunter"])
        path_seed = rng.randint(0, 10**9)
        path, level, _ = st.solve(mix, path_seed)
        if gives_way and level > 0:
            set_fight_cap(None)
            path, level, _ = st.solve(mix, path_seed)
            set_fight_cap(cap)
        if any(path.count(t) != k for t, k in mix.items()):
            raise RuntimeError(f"lost points for {mix}")
        paths.append("".join(path))
        levels.append(level)
    set_fight_cap(None)
    return paths, levels


def worst_paths(paths, levels):
    key = lambda pair: (st.max_run(pair[0], "ME"), st.max_run(pair[0], "ESTMU"))
    return sorted(zip(paths, levels), key=key, reverse=True)[:WORST_SIZE]


def shop_jitter_check(trials=20000):
    print("shop jitter on a 60-point path | longest stretch without a shop, p95/worst, for 3 shops and for 2")
    rng = random.Random(8)
    for jitter in (0.3, 0.15, 0.0):
        row = []
        for shops in (3, 2):
            width = 58 / shops
            runs = []
            for _ in range(trials):
                spots = sorted(1 + width * (i + 0.5 + rng.uniform(-jitter, jitter)) for i in range(shops))
                edges = [0] + spots + [60]
                runs.append(max(b - a for a, b in zip(edges, edges[1:])))
            row.append(f"{percentile(runs, 0.95):.0f}/{max(runs):.0f}")
        print(f"  +/-{jitter} | {row[0]} | {row[1]}")


def main():
    count = int(sys.argv[1]) if len(sys.argv) > 1 else 1500
    shop_jitter_check()
    data = {}
    print("case | full rules | cap met | no-rest p95/worst | fights p95/worst | no-shop p95/worst | adjacent chests")
    for an, act in ACTS.items():
        for mn, mode in MODES.items():
            for vn, (cap, gives_way) in VARIANTS.items():
                paths, levels = batch(act, mode, cap, gives_way, count, st.hash_name(an + mn))
                m = metrics(paths, levels, cap)
                data[f"{an}|{mn}|{vn}"] = dict(
                    sample=list(zip(paths[:SAMPLE_SIZE], levels[:SAMPLE_SIZE])),
                    worst=worst_paths(paths, levels),
                    metrics=m,
                )
                t = m["table"]
                r, f, s = (t[k] for k in ("Longest stretch without a rest site", "Most fights in a row", "Longest stretch without a shop"))
                print(
                    f"{an} / {mn} / {vn} | {m['full_rules']:.1%} | {m['cap_met']} | {r['p95']}/{r['worst']} | "
                    f"{f['p95']}/{f['worst']} | {s['p95']}/{s['worst']} | {m['adjacent_chests']:.0%}"
                )
    page = PAGE.replace("__DATA__", json.dumps(data, separators=(",", ":"))).replace("__COUNT__", str(count))
    (HERE / "08-preview-sample-paths.html").write_text(page)


PAGE = """<!doctype html>
<html><head><meta charset="utf-8"><title>PROTOTYPE: sample unified paths</title>
<style>
:root { color-scheme: light dark; }
body { font: 14px/1.45 system-ui, sans-serif; max-width: 1040px; margin: 24px auto; padding: 0 16px; }
h1 { font-size: 20px; margin: 0 0 4px; } h2 { font-size: 15px; margin: 22px 0 8px; }
.muted { opacity: .7; }
.bar { display: flex; flex-wrap: wrap; gap: 6px; margin: 6px 0; }
button { font: inherit; padding: 5px 10px; border: 1px solid #8888; border-radius: 6px; background: none; color: inherit; cursor: pointer; }
button.on { background: #3b6fd4; border-color: #3b6fd4; color: #fff; }
table { border-collapse: collapse; } td, th { padding: 3px 14px 3px 0; text-align: left; font-variant-numeric: tabular-nums; }
th { font-weight: 600; } tr + tr td { border-top: 1px solid #8883; }
.path { display: grid; gap: 1px; margin-bottom: 3px; }
.path i { font: 600 9px/14px ui-monospace, monospace; height: 14px; text-align: center; font-style: normal; border-radius: 2px; overflow: hidden; }
.M { background: #6b7280; color: #fff; } .U { background: #c4b5fd; color: #1e1b4b; }
.E { background: #b91c1c; color: #fff; } .R { background: #15803d; color: #fff; }
.S { background: #1d4ed8; color: #fff; } .T { background: #facc15; color: #422006; }
.legend i { display: inline-block; width: 16px; text-align: center; font: 600 10px/16px ui-monospace, monospace; font-style: normal; border-radius: 2px; margin: 0 4px 0 12px; }
.note { font-size: 12px; margin: 0 0 8px; }
.tag { font-size: 11px; color: #b45309; margin-left: 6px; }
.walk { border: 1px solid #8884; border-radius: 8px; padding: 10px 12px; }
</style></head><body>
<h1>Sample unified paths under the ordering rules (prototype)</h1>
<p>Do paths for each act read as balanced when you look at a batch of them, and which rules need tightening or loosening? Each row below is one act, from position 1 to the last point before the boss. Press the buttons to change the act, the difficulty, and the candidate rule.</p>
<p class="muted">Throwaway. The paths come from the ticket 06 ordering model. Point counts are modeled from the decompile and have not been measured in game. Each case ran __COUNT__ paths.</p>
<div class="legend"><i class="M" style="margin-left:0">M</i>Monster<i class="E">E</i>Elite<i class="R">R</i>Rest site<i class="S">S</i>Shop<i class="T">T</i>Treasure<i class="U">U</i>Unknown</div>
<h2>Choose a case</h2>
<div class="bar" id="acts"></div><div class="bar" id="modes"></div><div class="bar" id="variants"></div><div class="bar" id="views"></div>
<h2 id="title"></h2>
<div id="metrics"></div>
<p class="note" id="summary"></p>
<div id="paths"></div>
<div class="bar"><button id="more">Show the next batch</button></div>
<h2>Guided walkthroughs</h2>
<div class="bar" id="tabs"></div>
<div class="walk"><p id="walktext" style="margin-top:0"></p><div class="bar" id="steps"></div></div>
<script>
const DATA = __DATA__;
const ACTS = ["Act 1 Overgrowth", "Act 2 Hive", "Act 3 Glory"];
const MODES = ["Ascension 0, 5 elites", "Ascension 1+, 8 elites", "BigGameHunter"];
const VARIANTS = ["Decided rules", "Fight cap 5, strict", "Fight cap 4, strict", "Fight cap 5, gives way first", "Fight cap 4, gives way first"];
const VIEWS = { sample: "A random batch", worst: "The worst paths of the run" };
const PAGE_SIZE = 12;
const state = { act: ACTS[0], mode: MODES[0], variant: VARIANTS[0], view: "sample", page: 0 };
const caseOf = (variant) => DATA[`${state.act}|${state.mode}|${variant}`];
const WALKS = [
  { tab: "A typical act", text: "An ordinary run at ascension 0. Look for the rhythm: a fight first, a rest last, elites and rest sites spread out, chests sometimes touching.", steps: [
    ["See Act 1", { act: ACTS[0], mode: MODES[0], variant: VARIANTS[0], view: "sample" }],
    ["See Act 2", { act: ACTS[1], mode: MODES[0], variant: VARIANTS[0], view: "sample" }],
    ["See Act 3", { act: ACTS[2], mode: MODES[0], variant: VARIANTS[0], view: "sample" }] ] },
  { tab: "Long runs of fights", text: "Under the decided rules a path can hold 7 or 8 fights in a row, because monsters and elites are capped separately. Compare the worst paths with a cap of 5 on the combined run.", steps: [
    ["Worst paths, decided rules", { act: ACTS[0], mode: MODES[1], variant: VARIANTS[0], view: "worst" }],
    ["Worst paths, cap of 5", { act: ACTS[0], mode: MODES[1], variant: VARIANTS[3], view: "worst" }] ] },
  { tab: "A strict cap fails badly", text: "A strict cap can't always be met. When it fails, the path drops every preference and loses its spread, and rest sites can end up far apart. If the cap gives way first, the path only keeps its long fight run. Watch the longest stretch without a rest site.", steps: [
    ["Strict cap of 4, Act 1 ascension 1+", { act: ACTS[0], mode: MODES[1], variant: VARIANTS[2], view: "worst" }],
    ["Strict cap of 5, Act 1 ascension 0", { act: ACTS[0], mode: MODES[0], variant: VARIANTS[1], view: "worst" }],
    ["Same case, cap gives way first", { act: ACTS[0], mode: MODES[0], variant: VARIANTS[3], view: "worst" }] ] },
  { tab: "BigGameHunter", text: "This modifier turns almost every monster into an elite, so most paths can't satisfy every rule. The spread is lost on those paths. The rows tagged in orange dropped the preferences.", steps: [
    ["Act 1 with the modifier", { act: ACTS[0], mode: MODES[2], variant: VARIANTS[0], view: "sample" }],
    ["Its worst paths", { act: ACTS[0], mode: MODES[2], variant: VARIANTS[0], view: "worst" }] ] },
];
let walk = 0, step = -1;
const NAMES = { M: "Monster", E: "Elite", R: "Rest site", S: "Shop", T: "Treasure", U: "Unknown" };

function group(id, items, value, set) {
  const el = document.getElementById(id);
  el.replaceChildren(...items.map(([key, label]) => {
    const b = document.createElement("button");
    b.textContent = label;
    b.className = key === value ? "on" : "";
    b.onclick = () => { set(key); render(); };
    return b;
  }));
}
function pick(key, value) { state[key] = value; state.page = 0; step = -1; }
function render() {
  group("acts", ACTS.map(a => [a, a]), state.act, v => pick("act", v));
  group("modes", MODES.map(a => [a, a]), state.mode, v => pick("mode", v));
  group("variants", VARIANTS.map(a => [a, a]), state.variant, v => pick("variant", v));
  group("views", Object.entries(VIEWS), state.view, v => pick("view", v));
  const c = caseOf(state.variant), base = caseOf(VARIANTS[0]);
  document.getElementById("title").textContent = `${state.act}, ${state.mode}, ${state.variant}`;
  const rows = Object.entries(c.metrics.table).map(([name, v]) => {
    const was = base.metrics.table[name];
    const changed = state.variant !== VARIANTS[0] && was.worst !== v.worst ? ` (decided rules: ${was.worst})` : "";
    return `<tr><td>${name}</td><td>${v.avg}</td><td>${v.p95}</td><td>${v.worst}${changed}</td></tr>`;
  }).join("");
  document.getElementById("metrics").innerHTML = `<table><tr><th>Measured over every path</th><th>Average</th><th>95th percentile</th><th>Worst</th></tr>${rows}</table>`;
  const capText = c.metrics.cap_met === null ? "" : ` ${(c.metrics.cap_met * 100).toFixed(1)}% stayed within the fight cap.`;
  document.getElementById("summary").textContent = `${(c.metrics.full_rules * 100).toFixed(1)}% of paths met every rule and preference.${capText} ${(c.metrics.adjacent_chests * 100).toFixed(0)}% had adjacent chests.`;
  const list = c[state.view];
  const shown = state.view === "worst" ? list : list.slice(state.page * PAGE_SIZE, (state.page + 1) * PAGE_SIZE);
  const width = Math.max(...shown.map(([p]) => p.length));
  document.getElementById("paths").innerHTML = shown.map(([p, level]) =>
    `<div class="path" style="grid-template-columns:repeat(${width},minmax(0,1fr))">` +
    [...p].map((t, i) => `<i class="${t}" title="Position ${i + 1}: ${NAMES[t]}">${t}</i>`).join("") + `</div>` +
    (level > 0 ? `<div class="tag">Preferences dropped on the path above</div>` : "")).join("");
  document.getElementById("more").style.display = state.view === "worst" ? "none" : "";
  group("tabs", WALKS.map((w, i) => [i, w.tab]), walk, i => { walk = i; step = -1; });
  document.getElementById("walktext").textContent = WALKS[walk].text;
  const steps = document.getElementById("steps");
  steps.replaceChildren(...WALKS[walk].steps.map(([label, set], i) => {
    const b = document.createElement("button");
    b.textContent = `${i + 1}. ${label}`;
    b.className = i === step ? "on" : "";
    b.onclick = () => { Object.assign(state, set, { page: 0 }); step = i; render(); };
    return b;
  }));
}
document.getElementById("more").onclick = () => {
  const total = caseOf(state.variant).sample.length;
  state.page = (state.page + 1) % Math.ceil(total / PAGE_SIZE);
  render();
};
render();
</script></body></html>
"""

if __name__ == "__main__":
    main()
