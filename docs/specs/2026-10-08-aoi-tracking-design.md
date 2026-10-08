# AoI Tracking Upgrade — Design

Date: 2026-10-08 · Scope: `Assets/Scripts/FeatureMapRaycaster.cs` (single file)

## Goal
Make the area-of-interest (AoI) log research-grade: reliable filenames, alignable
timestamps, a raw trace for post-hoc re-analysis, debounced area transitions, and
fixation-level semantics for gaze behavior analysis.

## Changes

### A: Hardening
- Filename `{yyyy-MM-dd-HH-mm-ss}-{participantId}-{scene}-aoi.csv`; `-raw` / `-fixations`
  suffixes for the other two files; existing files get `-1`, `-2` instead of being overwritten.
- `participantId` serialized field (default `P01`).
- Transitions log columns: `StartEpochMs;LogTime;DurationInSec;Area;HitObject`
  (epoch-ms aligns with eye-tracking CSVs; HitObject identifies the box).
- Raw trace `…-aoi-raw.csv`: area sampled at 10 Hz — `EpochMs;LogTime;Area`.
- Debounce: a new area must hold ≥ `minDwell` (100 ms) before the switch commits;
  border-texel flicker no longer fragments the log.

### B: Fixation semantics (ported concept from GazeEventDetection)
- Angular velocity from successive camera-forward directions; > `saccadeVelocity`
  (50°/s) = saccade → ends the current fixation.
- Fixations ≥ `minFixationDuration` (100 ms) are committed to `…-aoi-fixations.csv`:
  `StartEpochMs;EndEpochMs;DurationInSec;Area`. Sub-threshold sweeps discarded.
- Area change during stable gaze closes the fixation; next stable sample starts a new one.

## Data flow
raycast → texel classify (dominant channel: red=Details, green=Advertisement,
blue=Logo, else None) → debounce → transitions + raw sample + fixation state machine.

## Error handling
Missing `PlayerCameraRoot` → one-time error, component disables (no per-frame NRE).
All writers opened in `Start`, closed in `OnDestroy`; open interval and open
fixation are closed there. Rows flushed per write.

## Out of scope
Data-driven AoI definitions (palette/ScriptableObjects), multi-object dedup beyond
the HitObject column, sync triggers with other loggers.

## Border hysteresis (added 2026-10-08, same day)

**Why.** The first real session (2026-10-08-08-47-20) showed the debounce alone is
not enough: while the ray sat on the Details/Advertisement border, small movements
alternated the committed area six times in ~1.1 s (130–350 ms holds). Each hold
exceeded `minDwell`, so the debounce correctly let them through — they are real
center-ray flips, not texel noise. Transitions-only logs fragmenting into such
alternation runs distort dwell analysis and inflate transition counts.

**How.** Schmitt-trigger hysteresis at switch time: before committing, a 5-ray
cross (center ray + 4 probes at `probeSpread` ≈ 1.7° left/right/up/down) is cast
and classified with the same pipeline. The switch commits only on ≥ 4/5 votes for
the new area; otherwise the dwell hold restarts (retry after another `minDwell`).

- A ray straddling a border splits its probes (typically 3/2) → never commits →
  the committed area stays on the side it entered from.
- A genuine area entry passes 5/5 on the first attempt → no added latency.
- Worst-case switch latency: `minDwell` + one retry cycle (≈ 200 ms).

**Knob.** `probeSpread` (default 0.03, angular fraction): larger = wider border
bands resist switching; smaller = borders commit sooner. Keep it well below the
angular size of the smallest area on screen.

**Not done.** Ring-majority on every frame (5 rays/frame always) — unnecessary:
verification only runs when a switch is already pending, so steady-state cost is
one extra raycast every ~100 ms.
