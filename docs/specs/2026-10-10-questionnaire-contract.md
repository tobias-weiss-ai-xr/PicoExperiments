# Contract — questionnaire public API & data schema (2026-10-10)

This is the *stable* surface of the modular questionnaire system. Consumers
(surveys, views, loggers, tests, external analysis) may rely on these names,
shapes and serialized fields. Anything not listed here is internal and may
change.

Version: **contract v1** (proposal). Semantic-versioned: additive changes are
minor; breaking changes bump the major and a migration note is required.

## 1. Authoring schema (JSON)

A survey is a `TextAsset` or embedded string. One survey per JSON document.

```json
{
  "id": "ssq",
  "title": "Simulator Sickness Questionnaire",
  "version": 1,
  "sections": [
    {
      "id": "nausea",
      "header": "Nausea",
      "items": [
        {
          "id": "nausea_a",
          "prompt": "Do you experience nausea ...?",
          "type": "slider",
          "required": true,
          "config": {
            "lowText": "Absent Feeling",
            "mediumText": "Moderate Feeling",
            "highText": "Extreme Feeling"
          }
        }
      ]
    }
  ]
}
```

Field rules:

- `id`: non-empty, unique within a survey. `[A-Za-z0-9_-]`, CSV-safe (no `;`, `\n`, `\r`).
- `type` must be a registered question type (see §3), else schema validation fails (fail-loud).
- `config` is a `Dictionary<string,string>` consumed by the concrete widget; unknown keys are ignored (forward-compatible).
- `required` defaults to `true`.

### Example survey assets (behavior parity with the existing SSQ)

- `Assets/Questionnaires/ssq.json` — the 6 SSQ stages currently hard-coded in
  `CsqQuestionnaire` (Nausea A/B, Vestibular A/B, Oculomotor A/B), each a
  `slider` with low/medium/high anchors. Last item config carries
  `"exitOnSubmit": "true"` to restore current `exit` behavior.
- Future: `presence.json`, `nasa-tlx.json`, `task-feedback.json`.

## 2. C# interfaces (the extension seams)

```csharp
namespace Questionnaire
{
    // Where a survey document comes from. Extensible (asset / REST / code).
    public interface ISurveyDataSource
    {
        Survey Load(ISurveyRegistry registry);
    }

    // One page/widget type: renders an item, validates, returns the value.
    public interface IQuestionWidget
    {
        string Type { get; }              // registry key, e.g. "slider"
        GameObject Build(Transform parent, Item item); // view = the only Unity-touching part
        bool IsComplete { get; }
        string ReadValue();                // serialized string value (CSV-safe)
        void Show(bool visible);
    }

    // Persists responses. CSV default; JSON/stream are drop-in adapters.
    public interface ISurveyLogger
    {
        void Open(string participantId, SceneInfo scene, string surveyId);
        void LogResponse(in SurveyResponse r);
        void Close();
    }
}
```

The runner orchestrator (`SurveyRunner`) is concrete and holds no MonoBehaviour
state that can't be injected; a fake widget / fake logger allow full headless
flow testing.

## 3. Question-type registry (v1)

| `type` | Widget | Response `value` |
|---|---|---|
| `slider` | 0–1 slider, low/medium/high anchors | `0.000..1.000` |
| `likert5` | 5 labeled buttons | `1..5` |
| `yesNo` | Yes/No buttons | `"yes"` / `"no"` |
| `singleChoice` | N option buttons | selected option `id` |
| `openText` | TMP input | trimmed string |

Registering a new type = implement `IQuestionWidget` + add one line to the
registry. No changes to schema validation, runner, or any existing widget.

## 4. Response record (in-memory)

```csharp
public readonly struct SurveyResponse
{
    public string SurveyId;
    public int    SurveyVersion;
    public string SectionId;
    public string ItemId;
    public string Type;
    public string Value;            // CSV-safe string
    public long   SubmittedAtEpochMs;
    public float  DurationSec;      // item presentation -> submit
}
```

## 5. Output contract (CSV)

One file per survey run, aligned with the existing `Recordings/` convention:
`Recordings/<yyyy-MM-dd-HH-mm-ss>-<scene>-questionnaire-<surveyId>.csv`

Canonical header (single row, `;`-separated, matching the project's existing
research-log style):

```
SubmittedAtEpochMs;LogTime;SurveyId;SurveyVersion;SectionId;ItemId;Type;Value;DurationSec;ParticipantId
```

Rules:

- One row per item, appended on submit (flush every ≤ 10 rows, mirroring the
  current logger).
- `LogTime` = human-readable local wall-clock (`HH:mm:ss.fff`), for parity with
  `aoi_report` streams.
- Value escaped: no `;`, `\r`, `\n` (single `CsvSafe` helper shared with
  `FeatureMapRaycaster`).
- A survey run that stops early still writes the rows submitted so far
  (`Close()` flushes; `OnApplicationPause`/`OnDestroy` both call `Close`).
- Optional JSON audit file (`…-survey.json`) per run containing the ordered
  items + responses — an alternative machine-readable sink, off by default.

## 6. Manifest / self-description

The survey run appends a `questionnaire` block to the same session knowledge if
a shared manifest is present (future); until then the CSV + JSON suffice and
are linkable to the AoI/gaze streams via `SubmittedAtEpochMs`.

## Compatibility

- **Migration parity gate:** the first authored `ssq.json` reproduces the exact
  stage order, prompts, anchors and next/exit flow of the current
  `CsqQuestionnaire`. A headless test asserts the authored survey equals the
  legacy list. This keeps the refactor behavior-identical.
- **Backward compat of files:** existing handwritten questionnaire CSVs are
  unchanged; the new format is a superset (adds `SurveyId/Version/ItemId;Type`).
