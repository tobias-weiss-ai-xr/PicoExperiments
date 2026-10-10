# Test pyramid — questionnaire system (2026-10-10)

Strategy for the modular questionnaire system (see
[modular design](2026-10-10-questionnaire-modular-design.md) and
[contract](2026-10-10-questionnaire-contract.md)). The pyramid is bottom-heavy:
most tests are pure and fast; the two Unity-dependent layers are thin and
few. Mirrors the project's existing CI-gate habit
(`analysis/ci_check_aoi_v4.py`).

```
            ▲
      ┌─────┴─────┐   E2E (PlayMode + manual device smoke)  —  ~2
      │    E2E    │   one real-prefab flow + one device pass/release
      ├───────────┤
   ┌──┴─────────┴─┐   Integration (EditMode, headless-ish) — ~12
   │ Integration  │   widget build vs mock canvas, runner walk with
   └──────────────┘   fake widgets/logger; ~1 per widget + ~3 runner
   ┌───────────────┐
   │  Unit (pure)  │   schema/validation, CSV format,
   └───────────────┘   ordering, round-trip, parity gate — ~25
```

## L1 — Unit (pure .NET / Unity EditMode) — the foundation

Target: `SurveyModel`, `SurveySchema`, `SurveyRunner` orchestration with fakes,
`CsvSurveyLogger` row formatting. No scene, no real UI. Run in Unity Test
Runner (EditMode) **and** as a headless `dotnet` self-test gate, closing the
"can't compile-verify outside the editor" gap the project already hits.

Cover:

- **Schema validation**: valid JSON → `Survey`; rejects unknown `type`; rejects
  empty/duplicate `id`; `required` default; unknown `config` keys ignored.
- **Round-trip**: `Survey` → JSON → `Survey` is equal.
- **Ordering**: runner visits sections/items in authored order.
- **Required gating**: a `required` item blocks advancing until complete; a
  `required=false` item may be skipped (with a recorded `skip` value or
  omission — contract decides; default: recorded as skipped row).
- **Value rules** per type: slider range, likert `1..5`, singleChoice ∈ options.
- **CSV row**: header exact-match against contract §5; escaping of `; \n \r`;
  epoch-ms monotonicity; flush cadence.
- **Parity gate**: authored `ssq.json` (6 items) equals the legacy hard-coded
  stage list (prompt, order, anchors, next/exit) — makes the refactor provably
  behavior-identical.
- **Data-source loading**: `TextAsset`/string → survey; missing asset = fail-loud.

## L2 — Integration (Unity EditMode tests)

Target: one concrete widget per type glued to a **mock** canvas (a minimal
in-memory widget harness), runner + an in-memory logger, scene view with a
stubbed transform/`TMP_Text`.

Cover:

- Each widget builds from its `config`, `IsComplete` transitions correctly, and
  `ReadValue()` returns the expected string.
- Runner walks a 3-item fake survey with a fake widget + in-memory logger,
  asserting order, required-block behaviour, and that every submit arrives at
  the logger with `SubmittedAtEpochMs` and `DurationSec >= 0`.
- `Close()` flushes and is idempotent; early-stop still emits prior rows.
- `ISurveyLogger` adapter contract: a fake sink records the same rows the CSV
  sink would.

These run with the Test Runner and need no device.

## L3 — E2E (PlayMode + manual)

Target: real `Questionnaire.unity` scene, real `Item` prefab subtree, real
`CsvSurveyLogger` writing to `Recordings/`.

Cover:

- One PlayMode test: load a tiny survey, run it through the real prefab flow
  (activate, answer, submit), assert the CSV exists and its rows match the
  contract header. This mirrors "report functions exercised end-to-end on a
  synthetic fixture before commit".
- Manual device smoke per release: launch, answer one full survey, verify the
  CSV opens on-device via `pull_device_recordings.py` and the header/rows are
  well-formed; confirm `OnApplicationPause` flushes when the HOME button
  backgrounds the app.

## Ordering / gate rule

The parity gate (L1) is a **blocking** pre-commit check, following the project's
heredoc-`&&` convention: if the refactor ever diverges from current SSQ output,
the commit is rejected. L1 runs in `dotnet`/EditMode as the fast static gate;
L2 runs in the Unity Test Runner; L3 is a manual/CI-on-demand PlayMode run
(slow, needs Unity editor only — no device required for the PlayMode test).

## Tooling

- **Domain in a separate assembly** (resolved 2026-10-10): the pure survey
  domain is split into its own assembly (referenced by the runtime *and* by a
  `dotnet test` project) so the L1 unit layer runs as a fast headless static
  gate with **no Unity editor and no HMD** — closing the project's
  "can't compile-verify outside the editor" gap. Newtonsoft is a shared
  reference at the same version as the Unity package.
- **Unity Test Framework** (`com.unity.test-framework`) — EditMode (L2) and
  PlayMode (L3) tests.
- Headless gate mirrors the existing `analysis/ci_check_aoi_v4.py` habit: run
  `dotnet test` on the domain assembly as the blocking commit check.
