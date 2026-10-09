#!/usr/bin/env python3
"""Static acceptance gates for the AoI v4 agentflow campaign (Windows-safe).

Usage (each exits 0 on pass, 1 on fail):
    python analysis/ci_check_aoi_v4.py heatmap
    python analysis/ci_check_aoi_v4.py transparency
    python analysis/ci_check_aoi_v4.py pull

All checks are static (editor closed): no Unity compile is possible, so gates
verify structure, symbols, schema stability, and scope discipline.
"""

import os
import py_compile
import subprocess
import sys

RAY = os.path.join("Assets", "Scripts", "FeatureMapRaycaster.cs")
SHADER = os.path.join("Assets", "Scripts", "FeatureMap.shader")
RAW_HEADER = "EpochMs;LogTime;Area;HitObject;PointX;PointY;PointZ;U;V"
TRANS_HEADER = "StartEpochMs;LogTime;DurationInSec;Area;HitObject"
FIX_HEADER = "StartEpochMs;EndEpochMs;DurationInSec;Area"


def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()


def cs_common():
    src = read(RAY)
    ok, why = [], []
    if src.count("{") == src.count("}"):
        ok.append("braces balanced")
    else:
        why.append("brace mismatch")
    for name, header in [("raw", RAW_HEADER), ("transitions", TRANS_HEADER), ("fixations", FIX_HEADER)]:
        if header in src:
            ok.append(f"{name} schema intact")
        else:
            why.append(f"{name} schema changed")
    if "probeSpread" in src:
        why.append("stale probeSpread")
    else:
        ok.append("no stale symbols")
    return ok, why, src


def shader_untouched():
    r = subprocess.run(["git", "diff", "--quiet", "HEAD", "--", SHADER])
    return r.returncode == 0


def check_heatmap():
    ok, why, src = cs_common()
    for sym in ["attentionHeatmap", "heatmapGain", '_EmissionMap', 'EnableKeyword("_EMISSION")', "Apply(false)"]:
        if sym in src:
            ok.append(f"has {sym}")
        else:
            why.append(f"missing {sym}")
    if shader_untouched():
        ok.append("shader untouched")
    else:
        why.append("FeatureMap.shader was modified (out of scope)")
    return ok, why


def check_transparency():
    ok, why, src = cs_common()
    if "RaycastAll" in src or "RaycastNonAlloc" in src:
        ok.append("all-hits query present")
    else:
        why.append("no RaycastAll/RaycastNonAlloc")
    if "3000" in src or "Transparent" in src or "transparent" in src:
        ok.append("transparent-hit skip logic present")
    else:
        why.append("no transparent detection")
    if shader_untouched():
        ok.append("shader untouched")
    else:
        why.append("FeatureMap.shader was modified (out of scope)")
    return ok, why


def check_pull():
    ok, why = [], []
    path = os.path.join("analysis", "pull_device_recordings.py")
    if os.path.exists(path):
        ok.append("helper exists")
    else:
        return ["-"], [f"missing {path}"]
    try:
        py_compile.compile(path, doraise=True)
        ok.append("py_compile")
    except py_compile.PyCompileError as e:
        why.append(f"compile error: {e}")
    r = subprocess.run([sys.executable, path, "--self-test"], capture_output=True, text=True)
    if r.returncode == 0:
        ok.append("--self-test exit 0")
    else:
        why.append(f"--self-test failed rc={r.returncode}: {r.stdout[-300:]} {r.stderr[-300:]}")
    return ok, why


CHECKS = {"heatmap": check_heatmap, "transparency": check_transparency, "pull": check_pull}


def main():
    if len(sys.argv) != 2 or sys.argv[1] not in CHECKS:
        sys.exit(__doc__)
    ok, why = CHECKS[sys.argv[1]]()
    for line in ok:
        print(f"OK   {line}")
    for line in why:
        print(f"FAIL {line}")
    sys.exit(0 if not why else 1)


if __name__ == "__main__":
    main()
