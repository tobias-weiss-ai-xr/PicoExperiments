using Questionnaire;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;

namespace Questionnaire.UI
{
    /// <summary>
    /// Single-choice from a list of options.
    /// Config expectations:
    ///   - "options": JSON array string of choices, e.g. '["Option A","Option B"]'
    ///   - OR individual: "option1", "option2", ... keys
    /// Value: selected option string (exact match from options list)
    /// </summary>
    public sealed class SingleChoiceWidget : MonoBehaviour, IQuestionWidget
    {
        [SerializeField] private ToggleGroup _toggleGroup;
        [SerializeField] private GameObject _optionPrefab;
        [SerializeField] private Transform _optionsContainer;

        private List<string> _options = new();
        private string _value = "";

        public string Type => "singleChoice";
        public bool IsComplete => !string.IsNullOrEmpty(_value);

        public void Configure(Item item)
        {
            _options.Clear();
            _value = "";

            // Destroy existing options
            foreach (Transform child in _optionsContainer ?? transform)
                Destroy(child.gameObject);

            // Parse options from config
            var rawOptions = GetOptionStrings(item);
            if (rawOptions.Count == 0)
            {
                Debug.LogWarning($"[SingleChoiceWidget] item '{item.id}' has no options");
                return;
            }

            _options = new List<string>(rawOptions);

            // Create toggle for each option
            if (_optionPrefab != null && _optionsContainer != null)
            {
                for (int i = 0; i < _options.Count; i++)
                {
                    var go = Instantiate(_optionPrefab, _optionsContainer, worldPositionStays: false);
                    var toggle = go.GetComponent<Toggle>();
                    var label = go.GetComponentInChildren<TMP_Text>(includeInactive: true);
                    if (label != null) label.text = _options[i];
                    if (toggle != null)
                    {
                        toggle.group = _toggleGroup;
                        toggle.onValueChanged.RemoveAllListeners();
                        int cap = i;
                        toggle.onValueChanged.AddListener(b => {
                            if (b) { _value = _options[cap]; }
                        });
                    }
                    go.SetActive(true);
                }
            }

            // Select first by default
            if (_options.Count > 0)
                _value = _options[0];
        }

        private List<string> GetOptionStrings(Item item)
        {
            if (item.config == null) return new List<string>();

            // Try "options" as JSON array
            if (item.config.TryGetValue("options", out var jsonOptions))
            {
                try {
                    return JArray.Parse(jsonOptions).Select(t => t.Value<string>()).ToList();
                } catch {}
            }

            // Fallback: take all config values that start with "option" as choices
            var options = new List<string>();
            for (int i = 1; ; i++)
            {
                if (item.config.TryGetValue("option" + i, out var opt) && !string.IsNullOrEmpty(opt))
                    options.Add(opt);
                else
                    break;
            }
            return options.Count > 0 ? options : new List<string>();
        }

        public string ReadValue() => _value;

        public void Show(bool visible) => gameObject.SetActive(visible);
    }
}
