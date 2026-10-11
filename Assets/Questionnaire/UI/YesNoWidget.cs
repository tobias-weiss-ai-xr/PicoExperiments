using Questionnaire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Questionnaire.UI
{
    /// <summary>
    /// Yes/No toggle widget. Renders two buttons or a single Toggle.
    /// Value: "yes" or "no".
    /// Config: yesText, noText (default "Yes"/"No")
    /// </summary>
    public sealed class YesNoWidget : MonoBehaviour, IQuestionWidget
    {
        [SerializeField] private Toggle _yesToggle;
        [SerializeField] private Toggle _noToggle;
        [SerializeField] private TMP_Text _yesLabel;
        [SerializeField] private TMP_Text _noLabel;

        private string _value = "yes";

        public string Type => "yesNo";
        public bool IsComplete => true;

        public void Configure(Item item)
        {
            if (_yesLabel != null) _yesLabel.text = item.config?.Get("yesText", "Yes");
            if (_noLabel != null)  _noLabel.text  = item.config?.Get("noText",  "No");

            if (_yesToggle != null)
            {
                _yesToggle.onValueChanged.RemoveAllListeners();
                _yesToggle.onValueChanged.AddListener(b => { if (b) _value = "yes"; });
            }
            if (_noToggle != null)
            {
                _noToggle.onValueChanged.RemoveAllListeners();
                _noToggle.onValueChanged.AddListener(b => { if (b) _value = "no"; });
            }

            // Default: Yes selected
            if (_yesToggle != null) _yesToggle.isOn = true;
            if (_noToggle != null)  _noToggle.isOn = false;
        }

        public string ReadValue() => _value;

        public void Show(bool visible) => gameObject.SetActive(visible);
    }
}
