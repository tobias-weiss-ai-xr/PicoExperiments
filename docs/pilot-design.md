# Pilot Study: Attention & Search in VR Product Rows

> Status: **pilot protocol** — goal is validating the AoI apparatus end-to-end
> and getting a first descriptive look at position and salience effects on
> visual search in a VR product row. Everything maps 1:1 onto existing
> pipeline outputs (`aoi.csv`, `-raw.csv`, `-fixations.csv`, `-tasks.csv`,
> `-perf.csv`, heatmap PNGs); no new code required.

## 1. Research question & pilot goals

The ObjectTracking scene shows five products in a row. A search task asks the
participant to look at a cued product while gaze is tracked at object level
(`wholeObjectAoi`). A **pilot** first answers *methodological* questions:

1. **Apparatus validity** — do trials start cleanly, does RT come out sane, do
   heatmaps accumulate at the target, do fixations land on the right objects?
2. **Perception** — is a target product reliably found (found rate >> 0)?
3. **First substantive look** — is search time (and hence gaze) affected by
   *position* in the row (edges vs. centre) and by *visual distinctness*
   (identical vs. colour-coded products)?

## 2. Design

Within-subject, repeated measures, two mini-blocks:

| Block | Factor manipulated | Levels | Trials |
|---|---|---|---|
| **A: position search** | Target position (slot 1…5) | 5 | 10 (each slot × 2) |
| **B: identity search** | None fixed (target = the coloured product) | 1 | 8 repetitions |

Order is counterbalanced: Block A ⇄ Block B alternated across participants.

**Controls that the pipeline already ships with:**
- `randomizeTrials` + `randomSeed` — trial order shuffled; the seed lands in
  every `tasks.csv` row (reproducibility).
- `shuffleBetweenTrials` — in Block B the products are re-arranged after every
  trial (`FeatureMapSpawner.ShuffleRow`), so position cannot be learned; slot
  colours travel with the product.
- `interTrialDelay` — 2 s between trials so the RT clock starts with a fresh
  gaze (avoids carry-over from the previous target).

## 3. Participants

- Target **n = 8** for the pilot (descriptive + Wilcoxon-level power only).
  Examine after n = 4 (stopping rule, §9).
- Inclusion: normal/corrected vision, able to stand comfortably for ~15 min.
- VR experience noted (first-time users get extra warm-up).
- Written consent; session ID = `participantId` on the raycaster (`P01`…).

## 4. Materials & apparatus

- PICO 4 Enterprise, Unity 6000.3.25f1, ObjectTracking scene.
- Five demo products in a row: `count = 5`, `spacing = 1.5`, `boxScale = 2`
  (row ≈ 6 m wide on the auto-fitted platform).

**Block B material setup** — assign 5 distinct materials to
`FeatureMapSpawner.slotMaterials[0..4]`, one of them high-contrast (e.g. red);
that slot's object is the search target throughout Block B (name follows the
object through shuffles — fixed target in JSON).

## 5. Inspector settings (locked for the pilot)

| Component | Setting | Value |
|---|---|---|
| FeatureMapRaycaster | `participantId` | `P01`… per participant |
| | `wholeObjectAoi` | ON — object names ARE the AOIs (`DemoBox_1..5`) |
| | `attentionHeatmap` | ON |
| | `saveHeatmapImage` | ON |
| | `logPerformance` | ON (validity gate, §8) |
| | `useEyeTracking` | OFF (head ray; toggle ON only for the calibration check) |
| | `minDwell` / `foveaRadius` / `saccadeVelocity` / `minFixationDuration` | 0.1 / 1.0 / 30 / 0.1 (defaults) |
| FeatureMapSpawner | `count` / `spacing` / `boxScale` | 5 / 1.5 / 2 |
| AoiTaskManager | `taskFile` | `position-search-v1.json` (A) / `identity-search-v1.json` (B) |
| | `interTrialDelay` | 2.0 |
| | `randomizeTrials` | ON |
| | `shuffleBetweenTrials` | OFF in A, ON in B |
| | `heatmapPerTrial` | ON |
| | `randomSeed` | 0 (clock seed; logged per row) |

Task files (already in the repo, `Assets/Experiments/`):

```json
// position-search-v1.json — 10 trials, each slot twice, search-then-randomize
{"trials":[
 {"target":"DemoBox_5","maxSearchSec":20},{"target":"DemoBox_2","maxSearchSec":20},
 {"target":"DemoBox_4","maxSearchSec":20},{"target":"DemoBox_1","maxSearchSec":20},
 {"target":"DemoBox_3","maxSearchSec":20},{"target":"DemoBox_2","maxSearchSec":20},
 {"target":"DemoBox_5","maxSearchSec":20},{"target":"DemoBox_1","maxSearchSec":20},
 {"target":"DemoBox_3","maxSearchSec":20},{"target":"DemoBox_4","maxSearchSec":20}]}

// identity-search-v1.json — 8 reps; target is the coloured product (fixed name)
{"trials":[
 {"target":"DemoBox_4","maxSearchSec":20},{"target":"DemoBox_4","maxSearchSec":20},
 {"target":"DemoBox_4","maxSearchSec":20},{"target":"DemoBox_4","maxSearchSec":20},
 {"target":"DemoBox_4","maxSearchSec":20},{"target":"DemoBox_4","maxSearchSec":20},
 {"target":"DemoBox_4","maxSearchSec":20},{"target":"DemoBox_4","maxSearchSec":20}]}
```

## 6. Procedure (~15 min)

1. Participant puts on the headset; run PICO eye-centre calibration once.
2. **Warm-up:** 2 untimed practice trials (target shown in HUD), participant
   looks at it, trial completes on lock-on. Confirm HUD (`trialDisplay`)
   readable and audio feedback audible.
3. **Block A** — 10 position trials. Trial starts after 2 s delay; HUD shows
   `Trial x/10 · Find: DemoBox_N`. Stop between trials.
4. **Short break** (~30 s): swap task file if crossing A⇄B, toggle
   `shuffleBetweenTrials`.
5. **Block B** — 8 identity trials (products rearranged each trial).
6. Exit; pull `Recordings/` (editor) or `adb pull …/files/Recordings/`.

## 7. Data outputs per session

| File | Answers |
|---|---|
| `<s>-aoi.csv` | dwell per object, order of visits |
| `<s>-aoi-raw.csv` | 10 Hz trace with hit points (post-hoc mapping) |
| `<s>-aoi-fixations.csv` | fixation count/rates per object |
| `<s>-aoi-tasks.csv` | **the primary outcome**: per-trial target, found?, search RT, seed |
| `<s>-aoi-perf.csv` | FPS percentile validity gate |
| `<s>-aoi-session.json` | manifest — full inspector state for replication |
| `<s>-heatmap-<obj>-trialN.png` | per-trial attention maps |

## 8. Analysis plan

All steps run with the existing tooling:

1. **Validity gates (per session, drop-free runs only):**
   - `aoi_report.py <s>` → `== perf`: p95 ≥ 70 fps, **no** `<40 fps`
     warning. Sessions below are flagged.
   - Sanity: fixations ≥ 30 over a 10-trial block; `tasks.csv` rows = trials.
2. **Descriptive (position, Block A):**
   - `aoi_report.py --batch Recordings/` → *pooled task results* table:
     found rate + mean RT **per target** (`DemoBox_1..5`).
   - Report each slot's mean/median RT, found rate, and TTFF to it from the
     transitions file.
3. **Descriptive (salience, Block B):**
   - Found rate + RT for the coloured target; heatmaps show whether the
     search concentrates on the distinct product (`heatmapPerTrial`).
4. **Planned comparisons** (report both, interpret with pilot-level caution):
   - Position: RT ranks across slots — Wilcoxon signed-rank (edge vs centre
     contrast, n ≤ 8) or clear descriptive pattern if too few.
   - A vs B: does distinct colouring cut search RT / raise found rate?
5. **Exclusion rules:** RT < 300 ms (reflex, not search), RT > 3× block median
   (inattention), trials whose perf gate fails, first-time-VR participants'
   first two trials.
6. **Deliverables per pilot batch:** per-session summary lines (batch mode),
   pooled per-target table, 2–4 illustrative heatmaps, one position plot.

## 9. Pre-registration-lite (pilot level)

- **H1:** Search RT differs across row positions (edges slower or centre
  slower — two-sided at pilot).
- **H2:** The distinct product in Block B is found faster and more often than
  the same nominal target in Block A.
- **H3 (apparatus):** ≥ 90 % of trials complete with found = true; heatmaps
  visually concentrate on the target.
- **Stopping rule:** examine at n = 4; stop if ≥ 2 of 4 sessions fail a
  validity gate (fix the apparatus, restart), else run to n = 8.

## 10. Risks & mitigation

| Risk | Mitigation |
|---|---|
| Eye/head tracking drift | `useEyeTracking` OFF in pilot; re-calibrate if precision visibly degrades |
| Frame drops → invalid data | `logPerformance` gate; drop flagged sessions from analysis |
| Trial starts with gaze already on target | `interTrialDelay = 2`; exclude RT < 300 ms |
| Shuffle leaves a product off-platform | `FitPlatform` centres + 10 % margin; visual check before Block B |
| Recentering on hitch | emission pre-warmed at spawn; watch HUD/judder; check `perf.csv` |

---
Links: `analysis/aoi_report.py`, `Assets/Experiments/*.json`,
`Assets/Scripts/{FeatureMapSpawner,FeatureMapRaycaster,AoiTaskManager}.cs`
