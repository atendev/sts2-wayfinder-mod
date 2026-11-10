#!/usr/bin/env python3
"""Enemy scaling curve prototype for the v0.2.0 map.

Prints projected Act 1-3 fight lengths and damage taken for candidate curves,
using the v0.1 baseline (00-v0-1-baseline.md), and writes
05-enemy-scaling-curve.html next to this file.

Projection model, deliberately crude:
  turns'  = turns  * mean HP factor over the act
  damage' = damage * mean HP factor * mean damage factor
It holds player power at v0.1 levels and ignores block, so it is an estimate of
what scaling alone does before the reward-rate cuts land.

Usage: 05-scaling-curve.py [--html PATH]
"""

import argparse
import json
from pathlib import Path

# Act -> fight type -> (fights, avg damage taken, avg turns), both Wayfinder baseline runs.
BASELINE = {
    1: {"Monster": [(30, 4.4, 3.1), (25, 6.2, 3.4)], "Elite": [(8, 6.4, 4.0), (5, 17.4, 4.2)], "Boss": [(1, 24, 5), (1, 21, 7)]},
    2: {"Monster": [(27, 2.5, 2.1), (22, 4.1, 2.0)], "Elite": [(8, 3.2, 1.9), (5, 5.0, 2.2)], "Boss": [(1, 0, 3), (1, 9, 1)]},
    3: {"Monster": [(21, 1.3, 1.4), (23, 1.9, 1.5)], "Elite": [(8, 2.9, 1.8), (5, 3.8, 1.6)], "Boss": [(1, 6, 4), (1, 21, 5)]},
}

# Vanilla per-fight ranges across the four vanilla baseline runs, for context.
VANILLA_ACT3 = {"Monster": "2.8-6.0 t, 3.5-17 dmg", "Elite": "4.0-8.0 t, 4-45 dmg", "Boss": "7-12 t, 0-34 dmg"}

TARGET_TURNS = 4.0
TARGET_DAMAGE = 15.0

# Each candidate: per act, (start, end) multipliers along the path for Monster,
# Elite and Unknown fights. Bosses use BOSS, one value per act.
CANDIDATES = {
    "A. Stepped per act": {
        "hp": [(1.00, 1.00), (1.45, 1.45), (2.35, 2.35)],
        "dmg": [(1.00, 1.00), (1.30, 1.30), (2.05, 2.05)],
    },
    "B. Smooth along the run": {
        "hp": [(1.00, 1.00), (1.00, 1.80), (1.80, 2.90)],
        "dmg": [(1.00, 1.00), (1.00, 1.50), (1.50, 2.60)],
    },
    "C. Step plus ramp": {
        "hp": [(1.00, 1.00), (1.25, 1.65), (2.00, 2.70)],
        "dmg": [(1.00, 1.00), (1.15, 1.45), (1.60, 2.50)],
    },
}

BOSS = {"hp": [1.00, 1.30, 1.50], "dmg": [1.00, 1.15, 1.25]}


def weighted(rows):
    n = sum(r[0] for r in rows)
    return n, sum(r[0] * r[1] for r in rows) / n, sum(r[0] * r[2] for r in rows) / n


def act_mean(pair):
    return (pair[0] + pair[1]) / 2


def project(candidate, act, kind):
    _, dmg, turns = weighted(BASELINE[act][kind])
    if kind == "Boss":
        h, d = BOSS["hp"][act - 1], BOSS["dmg"][act - 1]
    else:
        h, d = act_mean(candidate["hp"][act - 1]), act_mean(candidate["dmg"][act - 1])
    return turns * h, dmg * h * d, h, d


def mark(turns, dmg):
    return "meets" if turns >= TARGET_TURNS and dmg >= TARGET_DAMAGE else "short"


def tables():
    lines = []
    lines.append("### Act 3 projection against the target (4+ turns, 15+ damage)\n")
    lines.append("| Curve | Fight | Baseline turns | Baseline dmg | HP x | Dmg x | Projected turns | Projected dmg | Target | Vanilla Act 3 |")
    lines.append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    for name, c in CANDIDATES.items():
        for kind in ("Monster", "Elite", "Boss"):
            _, bd, bt = weighted(BASELINE[3][kind])
            t, d, h, m = project(c, 3, kind)
            target = mark(t, d) if kind != "Monster" else "n/a"
            lines.append(f"| {name} | {kind} | {bt:.1f} | {bd:.1f} | {h:.2f} | {m:.2f} | {t:.1f} | {d:.1f} | {target} | {VANILLA_ACT3[kind]} |")
    lines.append("")
    lines.append("### Every act, Elites and bosses\n")
    lines.append("| Curve | Act | Elite turns | Elite dmg | Boss turns | Boss dmg |")
    lines.append("| --- | --- | --- | --- | --- | --- |")
    for name, c in CANDIDATES.items():
        for act in (1, 2, 3):
            et, ed, _, _ = project(c, act, "Elite")
            bt, bd, _, _ = project(c, act, "Boss")
            lines.append(f"| {name} | {act} | {et:.1f} | {ed:.1f} | {bt:.1f} | {bd:.1f} |")
    return "\n".join(lines)


HTML = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Enemy scaling curve candidates</title>
<style>
.viz-root {
  color-scheme: light;
  --surface-1: #fcfcfb;
  --text-primary: #0b0b0b;
  --text-secondary: #52514e;
  --grid: #e4e3df;
  --series-1: #2a78d6;
  --series-2: #eb6834;
  --series-3: #1baf7a;
}
@media (prefers-color-scheme: dark) {
  :root:where(:not([data-theme="light"])) .viz-root {
    color-scheme: dark;
    --surface-1: #1a1a19;
    --text-primary: #f0efec;
    --text-secondary: #c3c2b7;
    --grid: #33322f;
    --series-1: #3987e5;
    --series-2: #d95926;
    --series-3: #199e70;
  }
}
:root[data-theme="dark"] .viz-root {
  color-scheme: dark;
  --surface-1: #1a1a19;
  --text-primary: #f0efec;
  --text-secondary: #c3c2b7;
  --grid: #33322f;
  --series-1: #3987e5;
  --series-2: #d95926;
  --series-3: #199e70;
}
body { margin: 0; }
.viz-root { background: var(--surface-1); color: var(--text-primary); font: 14px/1.4 system-ui, sans-serif; padding: 24px; }
h1 { font-size: 20px; margin: 0 0 4px; }
h2 { font-size: 15px; margin: 24px 0 4px; }
p.sub { color: var(--text-secondary); margin: 0 0 12px; max-width: 70ch; }
.legend { display: flex; gap: 16px; flex-wrap: wrap; margin: 8px 0; color: var(--text-secondary); }
.legend span::before { content: ""; display: inline-block; width: 14px; height: 2px; vertical-align: middle; margin-right: 6px; background: var(--c); }
.charts { display: flex; gap: 24px; flex-wrap: wrap; }
.chart { position: relative; }
svg text { fill: var(--text-secondary); font-size: 11px; }
svg .grid { stroke: var(--grid); stroke-width: 1; }
svg .series { fill: none; stroke-width: 2; stroke-linejoin: round; }
svg .boss { stroke: var(--surface-1); stroke-width: 2; fill: var(--text-primary); }
svg .xhair { stroke: var(--text-secondary); stroke-width: 1; stroke-dasharray: 3 3; }
.tip { position: absolute; pointer-events: none; background: var(--surface-1); border: 1px solid var(--grid); border-radius: 6px; padding: 6px 8px; font-size: 12px; white-space: nowrap; display: none; box-shadow: 0 2px 8px rgba(0,0,0,.12); }
.tip b { display: block; margin-bottom: 2px; }
.tip i { display: inline-block; width: 10px; height: 2px; background: var(--c); vertical-align: middle; margin-right: 6px; }
table { border-collapse: collapse; margin-top: 8px; font-variant-numeric: tabular-nums; }
th, td { border-bottom: 1px solid var(--grid); padding: 4px 10px; text-align: right; }
th:first-child, td:first-child, th:nth-child(2), td:nth-child(2) { text-align: left; }
td.meets { font-weight: 600; }
</style>
</head>
<body>
<div class="viz-root">
<h1>Enemy scaling curve candidates</h1>
<p class="sub">Multipliers on enemy HP and attack damage for Monster, Elite and Unknown fights, across the run. Dots mark the boss multiplier at the end of each act, the same for every candidate. 1.0 is vanilla.</p>
<div class="legend" id="legend"></div>
<div class="charts" id="charts"></div>
<h2>Projected Act 3 fights</h2>
<p class="sub">turns = baseline turns &times; mean HP multiplier for the act; damage = baseline damage &times; mean HP &times; mean damage multiplier. Player power held at v0.1 and block ignored, so this is what scaling alone does before the reward cuts.</p>
<div id="table"></div>
</div>
<script>
const DATA = __DATA__;
const COLORS = ["var(--series-1)", "var(--series-2)", "var(--series-3)"];
const W = 460, H = 260, M = {l: 40, r: 12, t: 12, b: 28};
const x = p => M.l + p / 3 * (W - M.l - M.r);
const NS = "http://www.w3.org/2000/svg";
const el = (tag, attrs, parent) => { const e = document.createElementNS(NS, tag); for (const k in attrs) e.setAttribute(k, attrs[k]); parent && parent.appendChild(e); return e; };
const valueAt = (acts, p) => { const a = Math.min(2, Math.floor(p)); const f = p - a; const [s, e] = acts[a]; return s + (e - s) * f; };

const legend = document.getElementById("legend");
DATA.names.forEach((n, i) => { const s = document.createElement("span"); s.style.setProperty("--c", COLORS[i]); s.textContent = n; legend.appendChild(s); });

for (const [key, title] of [["hp", "Enemy HP multiplier"], ["dmg", "Enemy damage multiplier"]]) {
  const wrap = document.createElement("div"); wrap.className = "chart";
  const h = document.createElement("h2"); h.textContent = title; wrap.appendChild(h);
  const svg = el("svg", {width: W, height: H, role: "img", "aria-label": title});
  const max = key === "hp" ? 3.0 : 2.6;
  const y = v => H - M.b - (v - 1) / (max - 1) * (H - M.t - M.b);
  for (let v = 1; v <= max + 1e-9; v += 0.2) {
    el("line", {class: "grid", x1: M.l, x2: W - M.r, y1: y(v), y2: y(v)}, svg);
    el("text", {x: M.l - 6, y: y(v) + 4, "text-anchor": "end"}, svg).textContent = v.toFixed(1);
  }
  ["Act 1", "Act 2", "Act 3", "End"].forEach((t, i) => el("text", {x: x(i), y: H - 8, "text-anchor": i === 0 ? "start" : i === 3 ? "end" : "middle"}, svg).textContent = t);
  DATA.names.forEach((n, i) => {
    const acts = DATA.candidates[n][key];
    let d = "";
    acts.forEach(([s, e], a) => { d += `${d ? "L" : "M"}${x(a)},${y(s)}L${x(a + 1)},${y(e)}`; });
    el("path", {class: "series", d, stroke: COLORS[i]}, svg);
  });
  DATA.boss[key].forEach((v, a) => el("circle", {class: "boss", cx: x(a + 1) - 4, cy: y(v), r: 4.5}, svg));
  const xh = el("line", {class: "xhair", y1: M.t, y2: H - M.b, visibility: "hidden"}, svg);
  const hit = el("rect", {x: M.l, y: M.t, width: W - M.l - M.r, height: H - M.t - M.b, fill: "transparent"}, svg);
  const tip = document.createElement("div"); tip.className = "tip";
  hit.addEventListener("mousemove", ev => {
    const r = svg.getBoundingClientRect();
    const px = ev.clientX - r.left;
    const p = Math.max(0, Math.min(2.999, (px - M.l) / (W - M.l - M.r) * 3));
    xh.setAttribute("x1", px); xh.setAttribute("x2", px); xh.setAttribute("visibility", "visible");
    const act = Math.floor(p) + 1, pct = Math.round((p - act + 1) * 100);
    tip.innerHTML = `<b>Act ${act}, ${pct}% along the path</b>` + DATA.names.map((n, i) => `<div style="--c:${COLORS[i]}"><i></i>${n}: ${valueAt(DATA.candidates[n][key], p).toFixed(2)}&times;</div>`).join("") + `<div>Boss Act ${act}: ${DATA.boss[key][act - 1].toFixed(2)}&times;</div>`;
    tip.style.display = "block";
    tip.style.left = Math.min(px + 12, W - 200) + "px";
    tip.style.top = (ev.clientY - r.top + 30) + "px";
  });
  hit.addEventListener("mouseleave", () => { tip.style.display = "none"; xh.setAttribute("visibility", "hidden"); });
  wrap.appendChild(svg); wrap.appendChild(tip);
  document.getElementById("charts").appendChild(wrap);
}

const t = document.createElement("table");
t.innerHTML = "<tr><th>Curve</th><th>Fight</th><th>Baseline turns</th><th>Baseline dmg</th><th>HP &times;</th><th>Dmg &times;</th><th>Projected turns</th><th>Projected dmg</th><th>Target</th></tr>" +
  DATA.rows.map(r => `<tr><td>${r[0]}</td><td>${r[1]}</td>${r.slice(2, 8).map(v => `<td>${v}</td>`).join("")}<td class="${r[8]}">${r[8]}</td></tr>`).join("");
document.getElementById("table").appendChild(t);
</script>
</body>
</html>
"""


def html():
    rows = []
    for name, c in CANDIDATES.items():
        for kind in ("Monster", "Elite", "Boss"):
            _, bd, bt = weighted(BASELINE[3][kind])
            t, d, h, m = project(c, 3, kind)
            rows.append([name, kind, f"{bt:.1f}", f"{bd:.1f}", f"{h:.2f}", f"{m:.2f}", f"{t:.1f}", f"{d:.1f}", mark(t, d) if kind != "Monster" else "n/a"])
    data = {"names": list(CANDIDATES), "candidates": CANDIDATES, "boss": BOSS, "rows": rows}
    return HTML.replace("__DATA__", json.dumps(data))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--html", default=str(Path(__file__).with_name("05-enemy-scaling-curve.html")))
    args = ap.parse_args()
    Path(args.html).write_text(html())
    print(tables())


if __name__ == "__main__":
    main()
