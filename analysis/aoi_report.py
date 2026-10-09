#!/usr/bin/env python3
"""Offline analysis for FeatureMapRaycaster AoI recordings.

Usage:
    python analysis/aoi_report.py Recordings/<date>-<pid>-<scene>-aoi
    python analysis/aoi_report.py --batch <dir>   # one line per *-aoi.csv session + aggregates

Reads the session's three CSVs (semicolon-separated, whichever exist):
    <prefix>.csv           transitions: StartEpochMs;LogTime;DurationInSec;Area;HitObject
    <prefix>-raw.csv       raw trace:   EpochMs;LogTime;Area[;HitObject;PointX;PointY;PointZ;U;V]
    <prefix>-fixations.csv fixations:   StartEpochMs;EndEpochMs;DurationInSec;Area

Prints dwell/fixation summaries and an area transition matrix. If the raw
trace has U/V columns, writes a 64x64 UV gaze histogram as
<prefix>-heatmap.png (needs matplotlib; skipped otherwise).
No third-party dependencies required for the text report.
"""

import csv
import glob
import math
import os
import statistics
import sys
from collections import Counter

HEATMAP_BINS = 64


def read_csv(path):
    with open(path, newline="", encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f, delimiter=";"))
    return rows


def fmt(x, nd=3):
    return f"{x:.{nd}f}"


def entropy_bits(counter):
    """Shannon entropy in bits over a probability distribution given as a Counter."""
    n = sum(counter.values())
    if n == 0:
        return 0.0
    return -sum((c / n) * math.log2(c / n) for c in counter.values())


def analyze_transitions(path):
    rows = read_csv(path)
    print(f"== transitions ({os.path.basename(path)}) ==")
    if not rows:
        print("(empty file)")
        return
    by_area = {}
    for r in rows:
        d = by_area.setdefault(r["Area"], [])
        d.append(float(r["DurationInSec"]))
    print(f"{'Area':<15}{'intervals':>10}{'total_s':>10}{'mean_s':>9}{'median_s':>10}")
    for a, durs in sorted(by_area.items(), key=lambda kv: -sum(kv[1])):
        print(f"{a:<15}{len(durs):>10}{fmt(sum(durs)):>10}{fmt(sum(durs)/len(durs)):>9}{fmt(statistics.median(durs)):>10}")
    total = sum(sum(d) for d in by_area.values())
    print(f"total session: {fmt(total)} s")

    # time-to-first-fixation per area, from first entry relative to session start
    t0 = min(int(r["StartEpochMs"]) for r in rows)
    first = {}
    for r in rows:
        first.setdefault(r["Area"], int(r["StartEpochMs"]))
    print("\ntime-to-first (TTFF), s from session start:")
    for a in sorted(first, key=lambda a: first[a]):
        print(f"  {a:<15}{(first[a] - t0) / 1000:>8.3f}")

    # per-object dwell + TTFF (multi-object runs: areas repeat across products)
    by_obj = {}
    first_obj = {}
    for r in rows:
        o = r["HitObject"] or "None"
        d = by_obj.setdefault(o, [0.0, 0])
        d[0] += float(r["DurationInSec"])
        d[1] += 1
        first_obj.setdefault(o, int(r["StartEpochMs"]))
    if len(by_obj) > 1 or "DemoBox" in "".join(by_obj):
        print("\nper-object dwell (HitObject):")
        print(f"{'Object':<20}{'intervals':>10}{'total_s':>10}{'ttff_s':>9}")
        for o, (tot, cnt) in sorted(by_obj.items(), key=lambda kv: -kv[1][0]):
            print(f"{o:<20}{cnt:>10}{fmt(tot):>10}{fmt((first_obj[o] - t0) / 1000):>9}")

    seq = [r["Area"] for r in rows]
    pairs = Counter(zip(seq, seq[1:]))
    areas = sorted({a for p in pairs for a in p})
    print("\ntransition matrix (rows: from, cols: to):")
    print("            " + "".join(f"{a[:11]:>13}" for a in areas))
    for a in areas:
        row = "".join(f"{pairs.get((a, b), 0):>13}" for b in areas)
        print(f"{a[:12]:<12} {row}")

    n = len(seq)
    ret = sum(1 for i in range(2, n) if seq[i] == seq[i - 2]) / max(1, n - 2)
    print("\nscanpath:")
    print(f"  sequence length: {n}")
    print(f"  distinct areas: {len(set(seq))}")
    print(f"  entropy (transition pairs): {fmt(entropy_bits(pairs))} bit")
    print(f"  immediate-return rate: {fmt(ret)}  (i>=2: area_i == area_i-2)")


def session_stats(prefix):
    """Return (dwell_by_area, total_s, fix_count) for one session prefix.
    None if the transitions CSV is missing; fix_count None if fixations CSV is missing."""
    if not os.path.exists(prefix + ".csv"):
        return None
    dwell = Counter()
    for r in read_csv(prefix + ".csv"):
        dwell[r["Area"]] += float(r["DurationInSec"])
    total = sum(dwell.values())
    fpath = prefix + "-fixations.csv"
    fix_count = len(read_csv(fpath)) if os.path.exists(fpath) else None
    return dwell, total, fix_count


def analyze_batch(directory):
    paths = sorted(glob.glob(os.path.join(directory, "*-aoi.csv")))
    if not paths:
        print("no *-aoi.csv session files found in", directory)
        return
    print(f"== batch: {len(paths)} session(s) in {directory} ==")
    sessions = []
    for p in paths:
        prefix = os.path.splitext(p)[0]
        name = os.path.basename(prefix)
        if name.endswith("-aoi"):
            name = name[:-4]
        st = session_stats(prefix)
        if st is None or not st[0]:
            print(f"  {name}: (no transitions data)")
            continue
        dwell, total, fix_count = st
        dom_area, dom_dur = max(dwell.items(), key=lambda kv: kv[1])
        dom_share = 100.0 * dom_dur / total if total else 0.0
        rate = 60.0 * fix_count / total if (total and fix_count) else 0.0
        print(f"  {name:<45}{fmt(total, 1):>8}s  dom {dom_area}({fmt(dom_share, 1)}%)  "
              f"fix {fix_count if fix_count is not None else 0}  {fmt(rate, 1)}/min")
        sessions.append(st)
    # pooled task results across sessions (the study-level output)
    task_pool = {}
    for p in paths:
        prefix = os.path.splitext(p)[0]
        tpath = prefix + "-tasks.csv"
        if not os.path.exists(tpath):
            continue
        for r in read_csv(tpath):
            targ = r["Target"] or "?"
            d = task_pool.setdefault(targ, [0, 0, []])
            d[0] += 1
            if r["Found"].strip().lower() == "true":
                d[1] += 1
                d[2].append(float(r["SearchSec"]))
    if task_pool:
        print("\npooled task results (all sessions):")
        print(f"{'Target':<16}{'n':>5}{'found':>7}{'rate%':>7}{'mean_rt_s':>11}")
        for targ, (n, found, fsecs) in sorted(task_pool.items(), key=lambda kv: -kv[1][0]):
            mean = fmt(sum(fsecs) / len(fsecs)) if fsecs else "-"
            print(f"{targ:<16}{n:>5}{found:>7}{fmt(100.0 * found / n, 1):>7}{mean:>11}")
    if not sessions:
        return
    totals = [t for _, t, _ in sessions]
    pooled = Counter()
    pooled_fix = 0
    n_fix = 0
    for dwell, _, fc in sessions:
        pooled.update(dwell)
        if fc is not None:
            pooled_fix += fc
            n_fix += 1
    grand = sum(pooled.values())
    print("\n-- aggregates over sessions --")
    print(f"  sessions: {len(sessions)}")
    print(f"  median session length: {fmt(statistics.median(totals), 1)} s")
    print("  pooled dwell shares:")
    for a, d in pooled.most_common():
        print(f"    {a:<15}{fmt(100.0 * d / grand, 1)}%")
    print(f"  pooled fixation count: {pooled_fix} (from {n_fix}/{len(sessions)} sessions)")


def analyze_fixations(path):
    rows = read_csv(path)
    print(f"\n== fixations ({os.path.basename(path)}) ==")
    if not rows:
        print("count: 0  (empty file)")
        return
    durs = [float(r["DurationInSec"]) for r in rows]
    per_area = Counter(r["Area"] for r in rows)
    print(f"\n== fixations ({os.path.basename(path)}) ==")
    print(f"count: {len(durs)}  total: {fmt(sum(durs))} s  "
          f"mean: {fmt(statistics.fmean(durs))} s  median: {fmt(statistics.median(durs))} s")
    for a, n in per_area.most_common():
        print(f"  {a:<15}{n}")


def analyze_raw(path, prefix):
    rows = read_csv(path)
    print(f"\n== raw trace ({os.path.basename(path)}) ==")
    n = len(rows)
    if n >= 2:
        span_s = (int(rows[-1]["EpochMs"]) - int(rows[0]["EpochMs"])) / 1000
        if span_s > 0:
            print(f"samples: {n}  span: {fmt(span_s, 1)} s  effective rate: {fmt(n / span_s, 2)} Hz")
        else:
            print(f"samples: {n}")
    shares = Counter(r["Area"] for r in rows)
    for a, c in shares.most_common():
        print(f"  {a:<15}{c:>6}  {fmt(100 * c / n, 1)}%")

    if not (n and "U" in rows[0] and rows[0]["U"] != ""):
        print("no U/V columns (pre-v2 recording) - heatmap skipped")
        return
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        import numpy as np
    except ImportError:
        print("matplotlib not installed - heatmap skipped")
        return
    uv = [(float(r["U"]), float(r["V"])) for r in rows if r["Area"] != "None"]
    if not uv:
        print("no feature-map hits - heatmap skipped")
        return
    grid, _, _ = np.histogram2d([u for u, _ in uv], [v for _, v in uv],
                                bins=HEATMAP_BINS, range=[[0, 1], [0, 1]])
    fig, ax = plt.subplots(figsize=(5, 5))
    ax.imshow(grid.T, origin="lower", cmap="inferno")
    ax.set_xlabel("U")
    ax.set_ylabel("V")
    ax.set_title(f"gaze density on feature map (n={len(uv)})")
    out = prefix + "-heatmap.png"
    fig.savefig(out, dpi=150, bbox_inches="tight")
    print(f"heatmap: {out}")


def analyze_tasks(path):
    rows = read_csv(path)
    print(f"\n== tasks ({os.path.basename(path)}) ==")
    if not rows:
        print("(empty)")
        return
    by_target = {}
    for r in rows:
        target = r["Target"] or "?"
        d = by_target.setdefault(target, [0, 0])
        d[0] += 1
        d[1] += 1 if r["Found"].strip().lower() == "true" else 0
    print(f"{'Target':<16}{'n':>5}{'found':>7}{'rate%':>7}{'mean_s':>9}{'median_s':>10}")
    for target, (n, found) in sorted(by_target.items()):
        found_times = [float(r["SearchSec"]) for r in rows
                       if r["Target"] == target and r["Found"].strip().lower() == "true"]
        mean_s = fmt(sum(found_times) / len(found_times)) if found_times else "-"
        med_s = fmt(statistics.median(found_times)) if found_times else "-"
        rate = 100.0 * found / n
        print(f"{target:<16}{n:>5}{found:>7}{fmt(rate, 1):>7}{mean_s:>9}{med_s:>10}")
    all_secs = [float(r["SearchSec"]) for r in rows if r["Found"].strip().lower() == "true"]
    if all_secs:
        print(f"overall found RT: n={len(all_secs)} mean={fmt(sum(all_secs) / len(all_secs))}s "
              f"median={fmt(statistics.median(all_secs))}s")


def analyze_scanpaths(tasks_path, raw_path):
    """Slice the 10 Hz raw trace by task-trial boundaries and print, per trial,
    the ordered object-visit path and how many objects were visited before the
    target (search efficiency)."""
    rows = read_csv(tasks_path)
    raw = read_csv(raw_path)
    if not rows or not raw:
        return
    print(f"\n== per-trial scanpath ({os.path.basename(tasks_path)}) ==")
    for ti, t in enumerate(rows):
        try:
            t0 = int(t["StartEpochMs"])
            rt = float(t["SearchSec"])
        except (KeyError, ValueError):
            continue
        t1 = t0 + int(rt * 1000) + 50  # found (or timeout) + small pad
        areas = []
        for r in raw:
            try:
                e = int(r["EpochMs"])
            except (KeyError, ValueError):
                continue
            if not (t0 <= e <= t1):
                continue
            a = r["Area"]
            if a and a != "None" and (not areas or areas[-1] != a):
                areas.append(a)
        target = t["Target"]
        pre = areas.index(target) if target in areas else -1
        found = t["Found"].strip().lower() == "true"
        path = " -> ".join(areas) if areas else "(no object hits)"
        print(f"  {ti + 1}: target={target} found={found} rt={fmt(rt)}s "
              f"pre_target_visits={pre}  {path}")


def analyze_perf(path):
    rows = read_csv(path)
    print(f"\n== perf ({os.path.basename(path)}) ==")
    if not rows:
        print("(empty)")
        return
    fps = sorted(float(r["Fps"]) for r in rows)
    n = len(fps)
    mn = min(fps)
    mean = sum(fps) / n
    med = fps[n // 2]
    p10 = fps[max(0, n // 10)]
    p50 = fps[n // 2]
    p95 = fps[min(n - 1, (95 * n) // 100)]
    mx = max(fps)
    print(f"n={n} min={fmt(mn, 1)} p10={fmt(p10, 1)} median={fmt(p50, 1)} "
          f"p95={fmt(p95, 1)} max={fmt(mx, 1)}fps (mean {fmt(mean, 1)})")
    if mn < 40:
        print("  WARNING: frames dropped below 40 fps - eye/head data validity at risk")


def main():
    args = sys.argv[1:]
    if not args:
        sys.exit(__doc__)
    if args[0] == "--batch":
        if len(args) != 2:
            sys.exit("usage: --batch <dir>")
        analyze_batch(args[1])
        return
    if len(args) != 1:
        sys.exit(__doc__)
    prefix = args[0].rstrip("/")
    if not prefix.endswith("-aoi") and os.path.exists(prefix + "-aoi.csv"):
        prefix += "-aoi"
    for suffix, fn in [(".csv", analyze_transitions),
                       ("-fixations.csv", analyze_fixations),
                       ("-raw.csv", analyze_raw),
                       ("-tasks.csv", analyze_tasks),
                       ("-perf.csv", analyze_perf)]:
        path = prefix + suffix
        if os.path.exists(path):
            fn(path, prefix) if suffix == "-raw.csv" else fn(path)
        else:
            print(f"(missing: {path})")

    # scanpath needs both the task boundaries and the raw trace
    tp = prefix + "-tasks.csv"
    rp = prefix + "-raw.csv"
    if os.path.exists(tp) and os.path.exists(rp):
        analyze_scanpaths(tp, rp)


if __name__ == "__main__":
    main()
