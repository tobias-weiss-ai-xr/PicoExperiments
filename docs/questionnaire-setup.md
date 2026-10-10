# Questionnaire Setup Guide

> **Increment 2** — Replace legacy `CsqQuestionnaire` with the modular `SurveySceneView` system.

## Overview

The new modular questionnaire system splits responsibilities:
- **Domain** (`Assets/Questionnaire/Domain/`): Pure C# models, schema parsing, validation, runner logic
- **UI** (`Assets/Questionnaire/UI/`): Unity adapters for logging, widgets, and scene view
- **Tests** (`tests/questionnaire/`): 21 xUnit tests for domain layer (`dotnet test`)

For legacy `CsqQuestionnaire` users, see migration below.

---

## Prerequisites

1. Unity editor with **Newtonsoft.Json** package installed
   - PICO projects already have this via Convai/GLTFUtility
2. `Assets/Questionnaire/` folder exists (from Increment 1+2 commits)
3. SSQ JSON file: `Assets/Questionnaires/ssq.json`

---

## Step 1: Create TextAsset for your survey JSON

Each survey lives in a **Unity TextAsset** so it can be assigned via Inspector.

### Creating the SSQ TextAsset:

1. **Create folders:**   
   `Assets/Resources/Questionnaires/`
2. **Create TextAsset:**   
   `Assets > Create > Text Asset` → name it `ssq.textasset` (or any name)
3. **Paste content:** Copy the content of `Assets/Questionnaires/ssq.json` into the TextAsset
4. **Assign to Source:**   
   `Assets > Create > Questionnaire > Survey Source` → name it `SsqSource`
   - In Inspector: set **Survey Asset** = `ssq.textasset`

> **Why TextAsset?** Allows assigning via drag-drop in Unity Inspector. JSON is deserialized by `SurveySchema.Parse()`.

### Authoring your own surveys:

```json
{
  "id": "my_survey",
  "title": "My Custom Survey",
  "version": 1,
  "sections": [
    {
      "id": "intro",
      "header": "Introduction",
      "items": [
        {
          "id": "welcome",
          "prompt": "Welcome! How are you today?",
          "type": "slider",
          "required": true,
          "config": {
            "lowText": "Not good",
            "mediumText": "OK",
            "highText": "Great",
            "exitOnSubmit": "false"
          }
        }
      ]
    }
  ]
}
```

**Supported `type` values:** `slider`, `likert5`, `yesNo`, `singleChoice`, `openText`
**Special `config` keys:**
- `exitOnSubmit: "true"` — completes survey after this item
- `lowText`, `mediumText`, `highText` — slider labels

**Validation rules:**
- `survey.id` must be present and CSV-safe (no `;`, `\r`, `\n`)
- `item.id` must be unique within survey
- `item.type` must be in known types list

---

## Step 2: Prepare your UI Prefab

The system expects a **template GameObject** with a specific hierarchy that matches the legacy `Item` prefab structure.

### Legacy CsqQuestionnaire hierarchy (your existing Item):

```
Item (GameObject)
├── Header Text (TMP_Text)
├── Modal Text (TMP_Text)
└── UI MinMaxSlider (GameObject)
    ├── Slider (UnityEngine.UI.Slider)
    ├── Low Text (TMP_Text)
    ├── Medium Text (TMP_Text)
    └── High Text (TMP_Text)
└── Buttons
    └── Submit (Button)
```

### Option A: Use existing Item prefab (recommended):
1. **Ensure hierarchy exists** as shown above
2. **Keep it inactive** in the questionnaire scene
3. **SurveySceneView will clone it** for each survey item

### Option B: Create new prefab:
1. **Create:** New UI GameObject with above structure
2. **Save as:** `Assets/Questionnaire/UI/Prefabs/ItemTemplate.prefab`
3. **Tags:** Add `QuestionnaireItem` tag for easier selection

> **SliderWidget note:** The UI MinMaxSlider is reused for slider-type items. `SliderWidget` component is optional — SurveySceneView can work without it for basic cases.

---

## Step 3: Set up SurveySceneView

### In your Questionnaire scene:

1. **Find or create** the questionnaire root GameObject (where `CsqQuestionnaire` used to be)
2. **Remove** the `CsqQuestionnaire` component (it's now a legacy shim)
3. **Add Component** → `Questionnaire/UI/SurveySceneView`
4. **Configure Inspector:**
   
   | Field | Value | Description |
   |-------|-------|-------------|
   | `_itemPrefab` | Drag: `Item` GameObject | Template for each question |
   | `_surveySource` | Drag: `SsqSource` asset | TextAssetSurveySource from Step 1 |
   | `_participantId` | (optional) | Participant identifier for CSV |
   | `_exitScene` | `"00_Menu"` | Scene to load after final submit |
   | `_autoStart` | `true` | Start survey automatically on Start |

5. **Add Component** → `Questionnaire/UI/CsvSurveyLogger` (optional if you want custom logger)
   -Leave empty for default logger (writes to `Recordings/`)

### Verify hierarchy auto-detection:
If you don't assign UI references in SurveySceneView, it will try to `Find()` them automatically:
- `_itemPrefab` → `transform.Find("Item")`
- `_headerText` → `transform.Find("Header Text")`
- `_modalText` → `transform.Find("Modal Text")`
- `_nextButton` → `transform.Find("Buttons/Submit")`

---

## Step 4: Test in Editor

1. **Play** the Questionnaire scene in Unity Editor
2. **Observe:**
   - First question appears
   - Slider works, labels show correctly
   - Submit advances to next question
   - Final submit exits to menu (or closes survey)
3. **Check CSV output:**   
   `Application.persistentDataPath/Recordings/<timestamp>-<scene>-questionnaire-<surveyId>.csv`

### Expected CSV header:
```csv
SubmittedAtEpochMs;LogTime;SurveyId;SurveyVersion;SectionId;ItemId;Type;Value;DurationSec;ParticipantId
```

### Example row:
```csv
1702845600000;10:30:45.123;ssq;1;nausea_a;nausea_a;slider;0.750;2.345;P001
```

---

## Step 5: Migrate from CsqQuestionnaire

If you have custom modifications to `CsqQuestionnaire.cs`:

| Legacy field | New equivalent |
|---------------|----------------|
| `stageList` | Survey JSON sections/items |
| `StartLogging()` | `CsvSurveyLogger.Open()` via SurveySceneView |
| `NextStage()` | SurveyRunner handles advancement |
| `CreateStage()` | SurveySceneView instantiates widgets |
| Column: `headerText` | CSV column: `SectionId` + `ItemId` |
| Column: `modalText` | CSV column: prompt in Item |
| Column: `rating` | CSV column: `Value` (from slider) |

### CSV output differences:
- **Legacy:** `headerText;modalText;rating`
- **New:** `SubmittedAtEpochMs;LogTime;SurveyId;SurveyVersion;SectionId;ItemId;Type;Value;DurationSec;ParticipantId`

> Migration tip: Use both systems in parallel temporarily, or convert recordings with a simple script.

---

## Directory Structure

```
Assets/
├── Questionnaire/
│   ├── Domain/
│   │   ├── CsvSafe.cs          # String escaping
│   │   ├── KnownQuestionTypes.cs
│   │   ├── LegacySsq.cs        # Parity fixture
│   │   ├── Ports.cs            # IQuestionWidget, ISurveyLogger, ISurveyDataSource
│   │   ├── Survey.cs           # POCOs: Survey, Section, Item
│   │   ├── SurveyExtensions.cs # Flatten helpers
│   │   ├── SurveyResponse.cs   # Struct for one response
│   │   ├── SurveyRunner.cs     # Orchestrates items + widgets + logger
│   │   ├── SurveySchema.cs     # Parse JSON + validate
│   │   └── Questionnaire.Domain.asmdef
│   └── UI/
│       ├── CsvSurveyLogger.cs  # Logs to Recordings/
│       ├── Likert5Widget.cs    # (coming)
│       ├── OpenTextWidget.cs   # (coming)
│       ├── YesNoWidget.cs      # (coming)
│       ├── SliderWidget.cs     # For slider type
│       ├── SingleChoiceWidget.cs # (coming)
│       ├── SurveySceneView.cs  # MonoBehaviour orchestrator
│       ├── TextAssetSurveySource.cs
│       └── Questionnaire.UI.asmdef
├── Questionnaires/
│   └── ssq.json                # Canonical JSON source
└── Resources/
    └── Questionnaires/
        └── ssq.textasset        # Unity TextAsset (optional: auto-generated)

docs/
└── questionnaire-setup.md      # This file

tests/
└── questionnaire/
    ├── CsvSafeTests.cs
    ├── ParityTests.cs
    ├── RunnerTests.cs
    ├── SchemaValidationTests.cs
    └── Questionnaire.Tests.csproj
```

---

## Troubleshooting

### "UILD FAILED: Unknown type XYZ"
- Add `XYZ` to `KnownQuestionTypes.V1` list
- Ensure widget class exists in `Assets/Questionnaire/UI/`

### No UI appears
- Check `_itemPrefab` is assigned and active
- Verify hierarchy matches expected names (`Header Text`, `Modal Text`, etc.)
- Check SurveySceneView GM is enabled

### CSV file not written
- Verify `Application.persistentDataPath/Recordings/` directory exists
- Check for exceptions in SurveySceneView console
- Ensure `CsvSurveyLogger.Close()` was called (happens on Submit of final item)

### Slider not moving
- Verify Slider component exists on `UI MinMaxSlider` GameObject
- Check it's not inhibited by CanvasGroup/Interactable

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    SurveySceneView                        │
│  (MonoBehaviour - Knowledge UI orchestrator)              │
├─────────────────────────────────────────────────────────┤
│  Loads Survey via:  TextAssetSurveySource                │
│  Creates:          SurveyRunner                           │
│  Assigns:          CsvSurveyLogger                        │
│  For each item:    Instantiate widget + capture value     │
└──────────────────────────────┬───────────────────────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         │                       │                       │
┌────────▼───────┐  ┌───────────▼───────┐  ┌─────────▼──────┐
│ SurveyRunner   │  │    Widgets        │  │  CsvLogger     │
│  (Domain)       │  │    (UI)           │  │   (UI)         │
├─────────────────┤  ├───────────────────┤  ├───────────────┤
│ - Walks items   │  │ - SliderWidget    │  │ - Writes CSV  │
│ - Manages index │  │ - Likert5Widget   │  │ - Flushes     │
│ - Tracks time   │  │ - YesNoWidget     │  │ - Closes       │
│ - Calls logger  │  │ - SingleChoice    │  │               │
└─────────────────┘  └───────────────────┘  └───────────────┘
         │
┌────────▼───────┐
│   Survey       │  (POCO)
│   Section       │
│   Item          │
└─────────────────┘
         │
┌────────▼───────┐
│   TextAsset    │  ← Assets/Resources/Questionnaires/ssq.textasset
└─────────────────┘
```

---

## References

- [Modular Design Spec](specs/2026-10-10-questionnaire-modular-design.md)
- [Public Contract](specs/2026-10-10-questionnaire-contract.md)
- [Test Pyramid](specs/2026-10-10-questionnaire-test-pyramid.md)
- [Legacy CsqQuestionnaire](Assets/Scripts/CsqQuestionnaire.cs) — now a shim
