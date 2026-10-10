// LEGACY SHIM — superseded by Questionnaire.UI.SurveySceneView + Domain layer.
//
// This file is kept to avoid scene deserialize breakage.
// Action: Replace CsqQuestionnaire on Questionnaire GameObjects with Questionnaire.UI.SurveySceneView
//         in the inspector, then remove this file once scenes are migrated.
//
// For a drop-in equivalent:
//   - Add QuestionnaireSceneView to the same GameObject
//   - Assign Item prefab to _itemPrefab
//   - Assign a TextAssetSurveySource to _surveySource (point to Assets/Questionnaires/ssq.json)
//   - Optionally set _exitScene to "00_Menu"
//
// See doc: docs/specs/2026-10-10-questionnaire-modular-design.md

using UnityEngine;

public class CsqQuestionnaire : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogError(
            "[CsqQuestionnaire] LEGACY: this component is now superceded by Questionnaire.UI.SurveySceneView. " +
            "Please add Questionnaire.UI.SurveySceneView to this GameObject and remove this script.");
    }
}
