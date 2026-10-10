using Questionnaire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Questionnaire.UI
{
    /// <summary>
    /// Five-point Likert scale widget. Renders 5 radio-style buttons.
    /// Config expectations: lowText, highText are optional end labels.
    /// Value stored as "1"-"5" string.
    /// </summary>
    public sealed class Likert5Widget : MonoBehaviour, IQuestionWidget
    {
        [SerializeField] private ToggleGroup _toggleGroup;
        [SerializeField] private Toggle[] _toggles = new Toggle[5];
        [SerializeField] private TMP_Text _lowLabel;
        [SerializeField] private TMP_Text _highLabel;

        private Item _item;
        private int _value = 3; // default to middle (3=neutral)

        public string Type => "likert5";
        public bool IsComplete => true;

        public void Configure(Item item)
        {
            _item = item;
            if (_lowLabel != null)  _lowLabel.text = item.config?.TryGetOrDefault("lowText",   "Strongly Disagree");
            if (_highLabel != null) _highLabel.text = item.config?.TryGetOrDefault("highText", "Strongly Agree");

            for (int i = 0; i < 5; i++)
            {
                if (_toggles[i] != null)
                {
                    _toggles[i].onValueChanged.RemoveAllListeners();
                    int val = i + 1;
                    _toggles[i].onValueChanged.AddListener((b) => { if (b) _value = val; });
                }
            }
            if (_toggles[2] != null) _toggles[2].isOn = true; // default to 3
        }

        public string ReadValue() => _value.ToString();

        public void Show(bool visible) => gameObject.SetActive(visible);
    }
}
