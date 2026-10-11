using Questionnaire;
using System.Collections.Generic;
using UnityEngine;

namespace Questionnaire.UI
{
    /// <summary>
    /// ScriptableObject for defining surveys directly in Unity Editor.
    /// Alternative to JSON TextAssets — easier to author, inspect, and modify.
    /// 
    /// Usage:
    ///   Right-click in Project window → Create → Questionnaire → Survey Asset
    ///   Configure sections and items in Inspector
    ///   Assign to SurveySceneView's _surveyAsset field
    /// </summary>
    [CreateAssetMenu(fileName = "Survey_", menuName = "Questionnaire/Survey Asset")]
    public sealed class SurveyAsset : ScriptableObject, ISurveyDataSource
    {
        [SerializeField] private string _id = "my_survey";
        [SerializeField] private string _title = "My Survey";
        [SerializeField] private int _version = 1;
        [SerializeField] private List<SectionData> _sections = new();

        [System.Serializable]
        public class SectionData
        {
            public string id = "section_1";
            public string header = "";
            public List<ItemData> items = new();
        }

        [System.Serializable]
        public class ItemData
        {
            public string id = "item_1";
            public string prompt = "Enter your answer";
            public string type = "openText";
            public bool required = true;
            public bool exitAfterSubmit = false;
            [Tooltip("For slider: exitOnSubmit=true. For others: custom config entries.")]
            public List<ConfigEntry> config = new();
        }

        [System.Serializable]
        public class ConfigEntry
        {
            public string key = "";
            public string value = "";
        }

        Survey ISurveyDataSource.Load()
        {
            var survey = new Survey
            {
                id = _id,
                title = _title,
                version = _version,
                sections = new List<Section>()
            };

            foreach (var secData in _sections)
            {
                var sec = new Section
                {
                    id = secData.id,
                    header = secData.header,
                    items = new List<Item>()
                };
                survey.sections.Add(sec);

                foreach (var itemData in secData.items)
                {
                    var item = new Item
                    {
                        id = itemData.id,
                        prompt = itemData.prompt,
                        type = itemData.type,
                        exitAfterSubmit = itemData.exitAfterSubmit,
                        required = itemData.required,
                        config = new Dictionary<string, string>()
                    };
                    foreach (var cfg in itemData.config)
                        if (!string.IsNullOrEmpty(cfg.key))
                            item.config[cfg.key] = cfg.value;
                    sec.items.Add(item);
                }
            }

            return survey;
        }

        // Create Survey from this asset (direct use)
        public Survey GetSurvey() => ((ISurveyDataSource)this).Load();

#if UNITY_EDITOR
        // Editor validation
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
                _id = name.Replace(" ", "_");
            
            foreach (var sec in _sections)
            {
                if (string.IsNullOrEmpty(sec.id))
                    sec.id = sec.header?.Replace(" ", "_") ?? "section";
                
                foreach (var item in sec.items)
                {
                    if (string.IsNullOrEmpty(item.id))
                        item.id = item.prompt?.Replace(" ", "_").Replace("?","").Replace("!","") ?? "item";
                    if (string.IsNullOrEmpty(item.type))
                        item.type = "openText";
                }
            }
        }
#endif
    }
}
