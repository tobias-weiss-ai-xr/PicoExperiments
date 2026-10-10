using Questionnaire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Questionnaire.UI
{
    /// <summary>
    /// Free-text input widget using TMP_InputField.
    /// Value: the raw text entered by user.
    /// Config: placeholder, characterLimit (optional)
    /// </summary>
    public sealed class OpenTextWidget : MonoBehaviour, IQuestionWidget
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private int _minLength = 0;
        [SerializeField] private bool _allowEmpty = true;

        public string Type => "openText";

        public bool IsComplete => _allowEmpty || (_inputField != null && _inputField.text.Length >= _minLength);

        public void Configure(Item item)
        {
            if (_inputField != null)
            {
                _inputField.text = "";
                _inputField.placeholder.GetComponent<TMP_Text>()?.text = 
                    item.config?.TryGetOrDefault("placeholder", "Enter your answer...");

                if (int.TryParse(item.config?.TryGetOrDefault("characterLimit", null), out int limit) && limit > 0)
                    _inputField.characterLimit = limit;
            }
        }

        public string ReadValue() => _inputField != null ? _inputField.text : "";

        public void Show(bool visible) => gameObject.SetActive(visible);
    }
}
