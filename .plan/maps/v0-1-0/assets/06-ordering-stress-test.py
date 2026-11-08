#!/usr/bin/env python3
"""Stress test for the ordering rules. Throwaway model of the rules, not the mod's engine.

Run: python3 -I 06-ordering-stress-test.py [paths_per_scenario]
Types: E elite, R rest site, S shop, T treasure, M monster, U unknown. Positions are 1-based.
"""
import collections
import itertools
import random
import sys
import time

TYPES = "ERSTMU"
MAX_LEVEL = 8
NODE_BUDGET = 50_000
RESTORE_RULES = True
RESTORE = (
    ("opening", True),
    ("e_gap", 2),
    ("e_gap", 1),
    ("t_cap", 3),
    ("rs_adj", True),
    ("rest_before_end", True),
)
LEVEL_NAMES = [
    "all rules and preferences",
    "preferences dropped",
    "rest allowed just before the last point",
    "back-to-back rest sites and shops allowed",
    "treasure run cap dropped",
    "elite gap shrunk to 1",
    "elite gap shrunk to 0 (still not touching)",
    "opening ban dropped",
    "back-to-back elites allowed",
]


def cfg(level):
    return dict(
        rest_before_end=level < 2,
        rs_adj=level < 3,
        t_cap=3 if level < 4 else None,
        e_gap=2 if level < 5 else 1 if level == 5 else 0,
        opening=level < 7,
        e_adj=level < 8,
    )


def broken_rules(p, mix):
    """Independent check: reads only the finished path."""
    n, out = len(p), set()
    if mix["M"] and p[0] != "M":
        out.add("first is fight")
    if mix["R"] and p[-1] != "R":
        out.add("last is rest")
    if any(c in "ERT" for i, c in enumerate(p[:5]) if not (i == n - 1 and c == "R")):
        out.add("opening ban")
    if mix["R"] and "R" in p[max(0, n - 3) : n - 1]:
        out.add("rest before end")
    if any(a == b and a in "RS" for a, b in zip(p, p[1:])):
        out.add("rest/shop adjacent")
    if max_run(p, "T") > 3:
        out.add("treasure cap")
    es = [i for i, c in enumerate(p) if c == "E"]
    gaps = [b - a - 1 for a, b in zip(es, es[1:])]
    if any(g == 0 for g in gaps):
        out.add("elites adjacent")
    if any(g < 2 for g in gaps):
        out.add("elite gap")
    return out


def allowed_broken(level):
    c, out = cfg(level), set()
    if not c["rest_before_end"]:
        out.add("rest before end")
    if not c["rs_adj"]:
        out.add("rest/shop adjacent")
    if c["t_cap"] is None:
        out.add("treasure cap")
    if c["e_gap"] < 2:
        out.add("elite gap")
    if not c["opening"]:
        out.add("opening ban")
    if not c["e_adj"]:
        out.add("elites adjacent")
    return out


def max_run(p, kinds):
    best = run = 0
    for c in p:
        run = run + 1 if c in kinds else 0
        best = max(best, run)
    return best


def spread_path(mix, rng, attempts=30):
    """Level 0: even spread with jitter, random treasure, capped filler."""
    n = sum(mix.values())
    if not mix["M"] or not mix["R"] or n < 8:
        return None
    for _ in range(attempts):
        p = [None] * n
        p[0], p[-1] = "M", "R"
        ok = True
        spec = (("E", 6, n - 1, mix["E"]), ("R", 6, n - 3, mix["R"] - 1), ("S", 2, n - 1, mix["S"]))
        for t, lo, hi, k in spec:
            if not k:
                continue
            if hi < lo:
                ok = False
                break
            width = (hi - lo + 1) / k
            for i in range(k):
                centre = lo - 1 + width * (i + 0.5 + rng.uniform(-0.3, 0.3))
                for pos in sorted(range(lo - 1, hi), key=lambda x: abs(x - centre)):
                    if p[pos] is not None:
                        continue
                    if pos > 0 and p[pos - 1] == t or pos < n - 1 and p[pos + 1] == t:
                        continue
                    if t == "E" and any(0 <= q < n and p[q] == "E" for q in (pos - 2, pos + 2)):
                        continue
                    p[pos] = t
                    break
                else:
                    ok = False
                    break
            if not ok:
                break
        if not ok:
            continue
        free = [i for i in range(5, n - 1) if p[i] is None]
        if len(free) < mix["T"]:
            continue
        for i in rng.sample(free, mix["T"]):
            p[i] = "T"
        filler = ["M"] * (mix["M"] - 1) + ["U"] * mix["U"]
        if filler.count("M") + filler.count("U") != sum(c is None for c in p):
            continue
        for _ in range(40):
            rng.shuffle(filler)
            it = iter(filler)
            q = [c if c else next(it) for c in p]
            if max_run(q, "M") <= 3 and max_run(q, "U") <= 3 and not broken_rules(q, mix):
                return q
    return None


def dfs_path(mix, c, rng):
    """Levels 1 and up: exact seeded search over hard rules, with a node budget."""
    n = sum(mix.values())
    fixed = {}
    counts = dict(E=mix["E"], R=mix["R"], S=mix["S"], T=mix["T"], F=mix["M"] + mix["U"])
    if mix["M"]:
        fixed[1] = "F"
        counts["F"] -= 1
    if mix["R"]:
        fixed[n] = "R"
        counts["R"] -= 1
    order = "ERSTF"
    dead, nodes, out = set(), [0], [None] * (n + 1)

    def ok(pos, t, prev, since_e, t_run):
        if c["opening"] and pos <= 5 and t in "ERT" and pos not in fixed:
            return False
        if c["rest_before_end"] and mix["R"] and t == "R" and pos in (n - 2, n - 1):
            return False
        if c["rs_adj"] and t in "RS" and prev == t:
            return False
        if t == "E" and c["e_adj"] and since_e is not None and since_e < c["e_gap"]:
            return False
        if t == "E" and c["e_adj"] and prev == "E":
            return False
        if t == "T" and c["t_cap"] and t_run + 1 > c["t_cap"]:
            return False
        return True

    def cannot_fit(pos):
        last_free = n - 1 if mix["R"] else n
        early = max(pos, 6) if c["opening"] else pos
        needs = []
        if c["e_adj"]:
            needs.append((counts["E"], early, last_free, max(c["e_gap"], 1)))
        if c["rs_adj"]:
            rest_end = last_free - (2 if c["rest_before_end"] and mix["R"] else 1 if mix["R"] else 0)
            needs.append((counts["R"], early, rest_end, 1))
            needs.append((counts["S"], pos, last_free, 1))
        return any(k and (k - 1) * (gap + 1) + 1 > hi - lo + 1 for k, lo, hi, gap in needs)

    def step(pos, prev, since_e, t_run):
        if pos > n:
            return True
        nodes[0] += 1
        if nodes[0] > NODE_BUDGET:
            raise TimeoutError
        key = (pos, tuple(counts[t] for t in order), prev, since_e, t_run)
        if key in dead:
            return False
        if cannot_fit(pos):
            dead.add(key)
            return False
        if pos in fixed:
            options = [fixed[pos]]
        else:
            weighted = [(rng.random() ** (1 / counts[t]), t) for t in order if counts[t]]
            options = [t for _, t in sorted(weighted, reverse=True)]
        for t in options:
            if not ok(pos, t, prev, since_e, t_run):
                continue
            if pos not in fixed:
                counts[t] -= 1
            out[pos] = t
            nse = 0 if t == "E" else None if since_e is None else min(since_e + 1, c["e_gap"] + 1)
            if t == "E":
                nse = 0
            if step(pos + 1, t, nse, t_run + 1 if t == "T" else 0):
                return True
            if pos not in fixed:
                counts[t] += 1
        dead.add(key)
        return False

    try:
        if not step(1, None, None, 0):
            return None, False
    except TimeoutError:
        return None, True
    filler = ["M"] * (mix["M"] - (1 if mix["M"] else 0)) + ["U"] * mix["U"]
    rng.shuffle(filler)
    it = iter(filler)
    path = []
    for pos in range(1, n + 1):
        t = out[pos]
        path.append(("M" if pos == 1 and mix["M"] else next(it)) if t == "F" else t)
    return path, False


def solve(mix, seed):
    rng = random.Random(seed)
    p = spread_path(mix, rng)
    if p:
        return p, 0, 0
    budget_hits = 0
    for level in range(1, MAX_LEVEL + 1):
        c = cfg(level)
        p, hit = dfs_path(mix, c, rng)
        budget_hits += hit
        if p:
            if RESTORE_RULES:
                for key, value in RESTORE:
                    if c[key] == value or (key == "e_gap" and c[key] >= value):
                        continue
                    trial = dict(c, **{key: value})
                    q, hit = dfs_path(mix, trial, rng)
                    budget_hits += hit
                    if q:
                        c, p = trial, q
            return p, level, budget_hits
    raise RuntimeError(f"no order for {mix}")


def mix_of(**kw):
    return {t: kw.get(t, 0) for t in TYPES}


def vanilla(rng, elites=5, big_game_hunter=False):
    while True:
        rooms, (rl, rh), (ul, uh) = rng.choice([(15, (6, 7), (10, 14)), (14, (6, 7), (9, 13)), (13, (5, 6), (9, 13))])
        widths = [rng.choices(range(1, 8), [2, 8, 16, 24, 24, 16, 10])[0] for _ in range(rooms)]
        first, treasure, last = max(2, widths[0]), widths[rooms - 7], widths[-1]
        n = max(sum(widths), 1)
        n = max(int(n * rng.uniform(0.8, 1.0)), first + treasure + last)
        rest = last + rng.randint(rl, rh) - (rng.randint(1, 2) if rng.random() < 0.2 else 0)
        elite = max(0, elites - (rng.random() < 0.2))
        shop = max(0, 3 - (rng.random() < 0.1))
        unknown = rng.randint(ul, uh)
        monster = n - rest - elite - shop - unknown - treasure
        if monster >= first:
            if big_game_hunter:
                elite += monster - first
                monster = first
            return mix_of(E=elite, R=rest, S=shop, T=treasure, M=monster, U=unknown)


def random_mix(rng, lo, hi):
    n = rng.randint(lo, hi)
    weights = [rng.expovariate(1) for _ in TYPES]
    picks = collections.Counter(rng.choices(TYPES, weights, k=n))
    return mix_of(**picks)


def elite_heavy(rng):
    n = rng.randint(30, 90)
    e = int(n * rng.uniform(0.3, 0.75))
    rest = collections.Counter(rng.choices("RSTMU", [3, 1, 1, 6, 3], k=n - e))
    return mix_of(E=e, **rest)


def one_type(rng):
    return mix_of(**{rng.choice(TYPES): rng.randint(1, 80)})


SCENARIOS = {
    "vanilla, 5 elites": lambda r: vanilla(r, 5),
    "vanilla, 8 elites": lambda r: vanilla(r, 8),
    "BigGameHunter": lambda r: vanilla(r, 5, True),
    "elite heavy (30-75%)": elite_heavy,
    "random mix, 26-90 points": lambda r: random_mix(r, 26, 90),
    "random mix, 1-25 points": lambda r: random_mix(r, 1, 25),
    "single type": one_type,
}


def stats_row(paths):
    def avg(values):
        return sum(values) / max(len(values), 1)

    if not paths:
        return "(no level 0 paths)"
    no_rest = [max_run(p, "ESTMU") for p in paths]
    fights = [max_run(p, "ME") for p in paths]
    t_adj = sum(max_run(p, "T") >= 2 for p in paths) / len(paths)
    t_max = max(max_run(p, "T") for p in paths)
    first_e = [p.index("E") + 1 for p in paths if "E" in p]
    return (
        f"no-rest stretch avg {avg(no_rest):.1f} worst {max(no_rest)}; "
        f"fight run avg {avg(fights):.1f} worst {max(fights)}; "
        f"adjacent chests {t_adj:.0%} longest {t_max}; first elite avg {avg(first_e):.1f}"
    )


def brute_force_check(trials=400):
    rng = random.Random(99)
    checked = mismatches = 0
    for _ in range(trials):
        mix = random_mix(rng, 1, 7)
        letters = "".join(t * k for t, k in mix.items())
        perms = {"".join(q) for q in itertools.permutations(letters)}
        def level_of(q):
            q = list(q)
            bad = broken_rules(q, mix)
            if "first is fight" in bad or "last is rest" in bad:
                return None
            return next(lv for lv in range(1, MAX_LEVEL + 1) if bad <= allowed_broken(lv))
        best = min(lv for lv in map(level_of, perms) if lv is not None)
        _, got, _ = solve(mix, rng.randint(0, 10**9))
        checked += 1
        if max(got, 1) != best:
            mismatches += 1
            print("  MISMATCH", mix, "solver", got, "brute", best)
    print(f"brute force on small mixes: {checked} checked, {mismatches} mismatches")


def main():
    count = int(sys.argv[1]) if len(sys.argv) > 1 else 500
    brute_force_check()
    for name, make in SCENARIOS.items():
        rng = random.Random(hash_name(name))
        levels, broken = collections.Counter(), collections.Counter()
        level0, sizes = [], []
        fails = budget = nondeterministic = 0
        start = time.time()
        for i in range(count):
            mix, seed = make(rng), rng.randint(0, 10**9)
            try:
                path, level, hits = solve(mix, seed)
            except RuntimeError:
                fails += 1
                continue
            bad = broken_rules(path, mix)
            bad_hard = bad - allowed_broken(level)
            counts_ok = collections.Counter(path) == collections.Counter({t: k for t, k in mix.items() if k})
            if bad_hard or not counts_ok:
                fails += 1
                print("  INVALID", mix, level, bad_hard, "".join(path))
            if i % 10 == 0 and solve(mix, seed)[0] != path:
                nondeterministic += 1
            levels[level] += 1
            broken.update(bad)
            budget += hits
            sizes.append(len(path))
            if level == 0:
                level0.append(path)
        print(f"\n## {name}: {count} paths, {min(sizes)}-{max(sizes)} points, {time.time() - start:.1f}s")
        print(f"failures {fails}; non-deterministic {nondeterministic}; search budget hit on {budget} levels")
        for lv in sorted(levels):
            print(f"  level {lv} ({LEVEL_NAMES[lv]}): {levels[lv] / count:.1%}")
        print("  rules broken: " + (", ".join(f"{k} {v / count:.1%}" for k, v in broken.most_common()) or "none"))
        print("  level 0 quality: " + stats_row(level0))


def hash_name(name):
    return sum(ord(c) * (i + 1) for i, c in enumerate(name))


if __name__ == "__main__":
    main()
