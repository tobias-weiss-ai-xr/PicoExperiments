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

## Decisions (resolved 2026-10-10)

- **JSON parser: Newtonsoft.Json.** Already a referenced Unity package
  (`com.unity.nuget.newtonsoft-json`) and already used on-device by
  Convai/GLTFUtility — zero new dependencies, IL2CPP/Android compatibility is
  proven in this codebase. Handles the `config {}` dictionary and future type
  extensibility that `JsonUtility` cannot (JsonUtility has no Dictionary /
  polymorphism support). `System.Text.Json` avoided: new dependency + AOT
  source-generator risk for no benefit at questionnaire data volume.
- **Domain lives in a separate assembly** (a `netstandard2.1`-compatible
  assembly referenced by the runtime and by a headless test project) so
  `dotnet test` / a fast static gate can run the pure layer without the
  Unity editor — closing the project's "can't compile-verify outside the
  editor" gap.

## Migration

Keep the existing SSQ stages byte-identical as the first authored survey asset
(`ssq.json`) enforced by a headless parity gate, so the refactor is
behavior-preserving.
