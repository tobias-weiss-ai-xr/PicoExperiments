# AoI v4 campaign plan (agentflow)

Three tasks executed by `af` (parallel workers, worktree-isolated, merged on
gate pass). This is the remaining backlog from the AoI v2/v3 research passes.
Editor is closed: acceptance gates are static checks; visual verification
happens at the next editor session.

## AH-1 — Live attention heatmap (emission overlay)

**Why.** Pilots and demos need to *see* accumulated attention on the product in
real time (Pfeiffer-style attention map). No new data, no schema change.

**How.** In `Assets/Scripts/FeatureMapRaycaster.cs` only — the shader is NOT to
be modified (it already exposes `_EmissionMap`, `_EmissionColor`, `_EMISSION`):

- New serialized knobs: `attentionHeatmap` (bool, default false), `heatmapGain`
  (float, default 1.0).
- When enabled: lazily create, per FeatureMap renderer hit, an accumulation
  buffer sized to that material's `_FeatureMap` and a black RGBA24 `Texture2D`;
  assign it to `material.SetTexture("_EmissionMap", tex)`, set `_EmissionColor`
  to a warm color (e.g. (1.5, 0.4, 0.15) HDR), `material.EnableKeyword("_EMISSION")`.
- Every classified hit: bump the texel at the hit UV (count, saturating).
- Repaint + `Apply(false)` on the raw-trace cadence (same clock as the 10 Hz
  raw sampler). Color ramp: black → red → yellow → white over
  `log2(1 + hits * heatmapGain)`.
- Decoding UV→texel: same convention as `ClassifyRay` (`uv.x * width`, clamp to
  `width-1`/`height-1`).
- Cleanup in `OnDestroy`: destroy created textures.

**Constraints.** No CSV schema change; no scene/meta/other-file edits; do not
touch `Assets/Scripts/FeatureMap.shader`.

**Gate.** `python analysis/ci_check_aoi_v4.py heatmap`

## AH-2 — Dual-ray transparency handling (deps: AH-1)

**Why.** MDPI Appl. Sci. 12:1027 refinement: "looking at or through?" — a ray
stopping on a transparent collider mislabels the AOI. Relevant for supermarket
scenes with glass.

**How.** In `Assets/Scripts/FeatureMapRaycaster.cs` only: replace the single
`Physics.Raycast` in `ClassifyRay` with an all-hits query; skip hits whose
renderer is transparent (material render queue ≥ 3000 or
`renderingMode == Transparent`), classify the first opaque hit. If only
transparent hits: report the first transparent hit as before (previous
behavior). Head-ray probes and cone voting use the same path automatically.

**Constraints.** Same as AH-1. `RaycastAll`/`RaycastNonAlloc` allocation kept
trivial (sort by distance, early out) — no per-frame GC pressure beyond a
reused buffer.

**Gate.** `python analysis/ci_check_aoi_v4.py transparency`

## AH-3 — Device recording retrieval helper

**Why.** On-device builds log to `Android/data/<package>/files/Recordings/`;
pulling them is manual adb archaeology.

**How.** New file `analysis/pull_device_recordings.py` (stdlib only): wraps
`adb pull` — `--adb <path>` (default "adb"), `--package <pkg>` (default
auto-detect via `adb shell pm list packages` filtered to known prefixes),
`--dest` (default `Recordings/device/`), `--list` (dry run), `--self-test`
(exercises parse/mapping logic with a synthetic package list — no adb needed).
ASCII output, cmd.exe-safe.

**Gate.** `python analysis/ci_check_aoi_v4.py pull` (runs `py_compile` +
`--self-test`).

## Out of scope (unchanged)

Distance-segmented colliders, session manifest extensions, CSV schema changes,
scene/hierarchy edits, shader edits.

## Campaign outcome (2026-10-08)

Executed via `af` (serialized after a pi-memory shared-store race crashed
parallel attempts; TF_MAX_PARALLEL=1). All three tasks passed their gates and
merged: 8bf376d (AH-1), 568b67a (AH-2), 1e2fca3 (AH-3).

Controller quality pass over the merged C# fixed four issues the static gates
could not see:

1. **Instant saturation** — the agent painted `val = 0 + gain` directly into
   the texture, so every looked-at texel went white on its first hit. Replaced
   with CPU-side count buffers + log-normalized black→red→white ramp painted
   on the raw-sample cadence (matches the plan's original ramp).
2. **Invisible heatmap** — `_EMISSION` was enabled but `_EmissionColor` left at
   its default black; added `SetColor("_EmissionColor", Color.white)`.
3. **RFloat texture** — single-channel format precluded the ramp; switched to
   RGBA32 with a cached pixel buffer (no GetPixels alloc per paint).
4. **Per-frame GC** — `RaycastAll` + closure `Array.Sort` on every
   classification ray replaced with a static reused buffer +
   `RaycastNonAlloc` + insertion sort.

The dual-ray logic (opaque preferred, transparent fallback) and the adb helper
passed review as merged.
