#!/usr/bin/env python3
"""Offline analysis for FeatureMapRaycaster AoI recordings.

Usage:
    python analysis/aoi_report.py Recordings/<date>-<pid>-<scene>-aoi

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


def analyze_transitions(path):
    rows = read_csv(path)
    print(f"== transitions ({os.path.basename(path)}) ==")
    by_area = {}
    for r in rows:
        d = by_area.setdefault(r["Area"], [])
        d.append(float(r["DurationInSec"]))
    print(f"{'Area':<15}{'intervals':>10}{'total_s':>10}{'mean_s':>9}{'median_s':>10}")
    for a, durs in sorted(by_area.items(), key=lambda kv: -sum(kv[1])):
        print(f"{a:<15}{len(durs):>10}{fmt(sum(durs)):>10}{fmt(sum(durs)/len(durs)):>9}{fmt(statistics.median(durs)):>10}")
    print(f"total session: {fmt(sum(sum(d) for d in by_area.values()))} s")

    seq = [r["Area"] for r in rows]
    pairs = Counter(zip(seq, seq[1:]))
    areas = sorted({a for p in pairs for a in p})
    print("\ntransition matrix (rows: from, cols: to):")
    print("            " + "".join(f"{a[:11]:>13}" for a in areas))
    for a in areas:
        row = "".join(f"{pairs.get((a, b), 0):>13}" for b in areas)
        print(f"{a[:12]:<12} {row}")


def analyze_fixations(path):
    rows = read_csv(path)
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
        print(f"samples: {n}  span: {fmt(span_s, 1)} s  effective rate: {fmt(n / span_s, 2)} Hz")
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


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    prefix = sys.argv[1].rstrip("/")
    if not prefix.endswith("-aoi") and os.path.exists(prefix + "-aoi.csv"):
        prefix += "-aoi"
    for suffix, fn in [(".csv", analyze_transitions),
                       ("-fixations.csv", analyze_fixations),
                       ("-raw.csv", analyze_raw)]:
        path = prefix + suffix
        if os.path.exists(path):
            fn(path, prefix) if suffix == "-raw.csv" else fn(path)
        else:
            print(f"(missing: {path})")


if __name__ == "__main__":
    main()
