# Spec — Modular, extensible questionnaires (2026-10-10)

Status: proposal (design phase, no code yet)
Scope: redesign of `Assets/Scripts/CsqQuestionnaire.cs` into a modular,
data-driven questionnaire system for VR experiment surveys (symptom scales,
presence, NASA-TLX-style questionnaires) that integrates with the project's
`Recordings/` research-data pipeline.

## Context

The current `CsqQuestionnaire` is a single 161-line script that:

- hard-codes its question list (`List<Stage> stageList`) inside the class;
- supports exactly one page layout: a 0–1 `UI MinMaxSlider` with three anchor
  labels (low/medium/high);
- wires UI by magic `transform.Find("UI MinMaxSlider")` paths to one fixed
  canvas subtree (`Item`), cloned per page;
- writes one CSV with three hard-coded columns (`headerText;modalText;rating`)
  named after the scene;
- has no branching, no required-field handling, no validation, and no way to
  mix question types (Likert / yes–no / single-choice / open text);
- is entirely Unity-coupled, so it cannot run in a headless test.

This blocks three concrete needs: (a) running several different surveys
(SSQ pre/post, presence, task-specific) in one experiment session; (b) adding
new question types and log formats without editing the runtime; (c) verifying
survey logic automatically before shipping an APK.

## Goals (in scope)

1. **Survey authored as data, not code.** A survey is a JSON document
   (embedded asset or `TextAsset`): `{ id, title, version, sections[].items[] }`.
   Adding a survey means adding a JSON file, not editing C#.
2. **Extensible question types.** A new question/response type plugs in via a
   registry entry — no changes to the runner or scene view.
3. **Extensible logging.** The runner is decoupled from persistence; the CSV
   logger is the default, an optional JSON/stream logger drops in.
4. **Composable surveys.** Pre/post batteries compose as separate surveys that
   can be chained in one session.
5. **Testable.** All decision logic lives in pure, headless-testable classes;
   only a thin view layer touches Unity transforms.
6. **Data contract.** Output aligns with the existing `Recordings/` naming and
   epoch-ms timestamps so questionnaire responses line up with AoI/gaze/perf
   streams.

## Non-goals (explicitly out)

- Localization/i18n engine (anchor strings are survey data; a translation is
  just another survey asset).
- Adaptive/branching engine beyond linear sections with an optional
  `condition` (see contract). Deep tree branching is deferred.
- Visual designer editor window. Authoring is by hand-editing JSON in v1.
- Full WCAG/accessibility audit (basic contrast/TMP sizing only).

## Architecture

Three layers with a single seam (ports & adapters, kept small):

```
 pure domain (headless-testable)        Unity view / IO (thin)
┌──────────────────────────────┐   ┌──────────────────────────────┐
│ SurveyModel  (data)          │   │ SurveySceneView              │
│ SurveyRunner (orchestrator)  │──▶│  · binds runner to VR canvas │
│ IQuestionWidget (port)       │◀──│  · builds the real widgets   │
│ ISurveyLogger  (port)        │   │  · owns the panel prefab tree│
│ SurveySchema (validation)    │   │                              │
└──────────────────────────────┘   └──────────────────────────────┘
        ▲ headless tests                ▲ EditMode/PlayMode tests
```

- **SurveyModel / SurveyRunner / SurveySchema** — pure C#, no `UnityEngine`
  runtime dependencies beyond the serialization `[System.Serializable]`
  classes. Unit-testable in the Unity Test Runner (EditMode) and in a plain
  `dotnet` run as a static gate.
- **Ports** (`IQuestionWidget`, `ISurveyLogger`, `ISurveyDataSource`) are the
  extension seams. The scene view and loggers are adapters.
- The old `CsqQuestionnaire` becomes `SurveySceneView` + one concrete
  `SliderWidget` + one concrete `CsvSurveyLogger`, preserving current behavior
  (SSQ stages, next/exit) as the default authored survey.

## Data model

- **Survey** `{ id, title, version, sections[] }`
- **Section** `{ id, header, items[] }`
- **Item** `{ id, prompt, type, required (default true), config{} }`
- **type** (v1): `slider` | `likert5` | `yesNo` | `singleChoice` | `openText`
  (extensible via registry)
- **Response** at submit time:
  `{ surveyId, surveyVersion, sectionId, itemId, type, value, submittedAtEpochMs, durationSec }`
- **Output** per survey run, `Recordings/<ts>-<scene>-questionnaire-<surveyId>.csv`
  with a canonical header (details in contract doc).

## Extensibility points (mapped to seams)

| Want to add | Touch | Never touch |
|---|---|---|
| New question type | new widget class + register type name | runner, logger, schema |
| New survey | new JSON asset | any code |
| New log format / sink | new `ISurveyLogger` adapter | runner, schema |
| New source (REST, CSV) | new `ISurveyDataSource` adapter | runner |
| New validation rule | `SurveySchema` validator list | runner contract |

## Open questions

- JSON parser choice: `JsonUtility` (built-in, no UnityEngine.Object-safe for
  generic lists — needs wrapper) vs `Newtonsoft` (already in the package set
  via HuggingFace API package) vs `System.Text.Json`. Constraint: must work on
  Android IL2CPP.
- Migration: keep the existing SSQ stages byte-identical as the first authored
  survey asset (parity gate), so the refactor is behavior-preserving.
