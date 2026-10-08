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

## AoI v2: research alignment (added 2026-10-08, same day)

Literature review of VR AoI/fixation methodology; three changes shipped, one
tool added. The 5-ray cross hysteresis above is superseded by foveal cone
voting; both are kept in git history.

**A. Fixation threshold corrected.** `saccadeVelocity` 50 → **30 °/s**
(inspector-tunable). Two 2025 validations of I-VT in VR with head rotation
included in gaze velocity find the optimum at 20–35 °/s (means 25.7/30.3 °/s;
IEEE VRW 2025 "Optimizing Velocity Thresholds for Fixation Detection in VR";
Wageningen thesis, same study), consistent with Tobii's 30 °/s recommendation.
50 °/s sat above the validated range and would over-merge fixations. The 100 ms
minimum duration already matches Tobii defaults and Llanes-Jurado et al.

**B. Foveal cone voting (view cone sampling).** The binary 5-ray cross is
replaced by a Gaussian-weighted cone: center ray + 8 rays at 0.5·`foveaRadius`
+ 8 at `foveaRadius` (weights exp(−θ²/2σ²), σ = r/2). A switch commits at
**≥ 75 %** of total weight, else the dwell hold restarts. Rationale:

- Single-ray sampling is the documented weak point of VR gaze pipelines;
  Gaussian ray bundles simulating the foveal receptive field are the current
  fix (arXiv 2601.02721, "View Cone Sampling").
- The 75 % bar is not arbitrary: with the center ray carrying full weight, a
  symmetric straddle scores ≈ 0.68, so 0.6 would let borders commit. Sim-verified
  (texel-grid port): straddle ≤ 0.68 rejected from both sides; ≥ 0.94 (center
  genuinely inside, ~1 texel clearance) commits first try.

Knob: `foveaRadius` in degrees (default 1.0) replaces `probeSpread`; larger =
more clearance needed to switch, smaller = earlier commits.

**C. Geometry in the raw trace.** Raw CSV columns extended to
`EpochMs;LogTime;Area;HitObject;PointX;PointY;PointZ;U;V` (world hit point, UV
on the hit surface; empty on miss). This is what post-hoc surface mapping
needs — fixation-density heatmaps on the mesh, per-participant AOI re-mapping,
replicable AOI reporting (PLUME surface mapping, arXiv 2601.07571; Hooge et al.
2026, "The fundamentals of eye tracking part 6"). Pre-v2 recordings lack the
columns; the analysis tool degrades gracefully.

**D. Offline analysis tool.** `analysis/aoi_report.py` (stdlib + optional
matplotlib): dwell/fixation summaries, area transition matrix, raw-trace rate
and shares, UV gaze histogram as `<prefix>-heatmap.png`.

**Still out of scope.** Dual raycaster for transparent surfaces and
distance-segmented colliders (MDPI Appl. Sci. 12:1027 workflow); real-time
surface attention accumulation; eye-tracking-based AOIs (this demo is
head-raycast by design).

## AoI v3: provenance, eye-ray source, smoothed I-VT (added 2026-10-08)

Three changes plus report upgrades, executed via subagent flow (one implementer
per file + two-stage review; the C# implementer was completed by the controller
after a stall, including one correctness fix: the switch-verification cone now
centers on the ray that proposed the area, `_gazeFwd`, not head forward).

**Session manifest.** Every session now writes `<base>-session.json`
(JsonUtility): pipeline id `aoi-v3`, participant, scene, timestamps, Unity
version, all pipeline knobs (minDwell, foveaRadius, rawSampleRate,
saccadeVelocity, minFixationDuration, useEyeTracking), and the feature-map
texture name/size. Recordings are self-describing - the Hooge et al. 2026
"report your AOI pipeline" requirement, enforced by construction.

**Eye-ray gaze source.** `useEyeTracking` (default false): when the scene's
EyeTrackingManager (PICO combined gaze, 24 Hz) reports valid data within the
last 150 ms, the classification ray, the verification cone, and the fixation
input all use the world-space eye-gaze direction; otherwise head forward. The
manager's event only fires on device-valid frames; freshness covers missed
frames. This upgrades the demo from "where the head pointed" to "where the
participant looked" on capable hardware, with head-ray fallback everywhere else.
Debug ray tint: blue = eye-driven, red/green = head mode.

**I-VT velocity smoothing.** The saccade threshold now applies to a sliding
~20 ms moving-average angular velocity (Tobii-style filter; per the IEEE VRW
2025 validation) instead of raw per-frame velocity, so frame-time jitter cannot
trip a false saccade.

**Report upgrades** (`analysis/aoi_report.py`): time-to-first-fixation per
area; `--batch <dir>` mode with per-session summary lines and pooled dwell /
fixation aggregates; scanpath basics (sequence length, distinct areas,
transition-pair Shannon entropy, immediate-return rate).

**Known minor** (documented, not fixed): in eye mode the cone ring offsets use
head-relative right/up axes; the 8-fold symmetric layout makes the skew
negligible at the ~1 deg ring radius. Revisit only if eye-in-head angles grow.
