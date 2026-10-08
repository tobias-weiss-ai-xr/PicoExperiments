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
