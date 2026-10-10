using Questionnaire;
using UnityEngine;

namespace Questionnaire.UI
{
    /// <summary>
    /// Loads a survey from a Unity TextAsset.
    /// </summary>
    [CreateAssetMenu(menuName = "Questionnaire/Survey Source", fileName = "SurveySource")]
    public sealed class TextAssetSurveySource : ScriptableObject, ISurveyDataSource
    {
        [SerializeField] private TextAsset _surveyAsset;

        public Survey Load()
        {
            if (_surveyAsset == null)
                throw new System.InvalidOperationException("TextAssetSurveySource: survey TextAsset is null");
            return SurveySchema.Parse(_surveyAsset.text);
        }
    }
}
