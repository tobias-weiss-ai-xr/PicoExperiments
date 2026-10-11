using Questionnaire;
using UnityEngine;

namespace Questionnaire.UI
{
    /// <summary>
    /// Simple survey source that loads from a TextAsset.
    /// Usage:
    ///   1. Create your survey JSON as a TextAsset (e.g., Assets/Resources/Questionnaires/my_survey.json)
    ///   2. In SurveySceneView, assign the TextAsset to the _surveyAsset field
    ///   3. OR use Resources.Load<TextAsset>("Questionnaires/my_survey") to get the asset
    /// </summary>
    [System.Obsolete("Use Assign TextAsset survey asset directly on SurveySceneView instead. This was a ScriptableObject approach that added unnecessary complexity.")]
    public sealed class TextAssetSurveySource : ScriptableObject, ISurveyDataSource
    {
        [SerializeField] private TextAsset _surveyAsset;

        public Survey Load()
        {
            if (_surveyAsset == null)
                throw new System.InvalidOperationException("TextAssetSurveySource: survey TextAsset is null. Assign one in Inspector.");
            if (string.IsNullOrEmpty(_surveyAsset.text))
                throw new System.InvalidOperationException("TextAssetSurveySource: TextAsset has no content.");
            return SurveySchema.Parse(_surveyAsset.text);
        }

        /// <summary>Static helper for direct TextAsset usage.</summary>
        public static Survey LoadFromAsset(TextAsset asset)
        {
            if (asset == null) throw new System.ArgumentNullException(nameof(asset));
            return SurveySchema.Parse(asset.text);
        }
    }
}
