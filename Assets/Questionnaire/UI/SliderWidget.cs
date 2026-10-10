using Questionnaire;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Questionnaire.UI
{
    /// <summary>
    /// Unity UI widget for slider-type items. Creates and drives the
    /// sliding control with low/medium/high anchor labels using the
    /// same hierarchy names as the legacy CsqQuestionnaire prefab.
    /// </summary>
    public sealed class SliderWidget : MonoBehaviour, IQuestionWidget
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _lowText;
        [SerializeField] private TMP_Text _mediumText;
        [SerializeField] private TMP_Text _highText;

        private Item _item;

        public string Type => "slider";
        public bool IsComplete => true; // Slider always provides a usable value 0-1

        public void Configure(Item item)
        {
            _item = item;
            _slider.value = 0.5f; // default middle

            if (_lowText != null)   _lowText.text = item.config?.TryGetOrDefault("lowText", "low");
            if (_mediumText != null) _mediumText.text = item.config?.TryGetOrDefault("mediumText", "medium");
            if (_highText != null)  _highText.text = item.config?.TryGetOrDefault("highText", "high");
        }

        public string ReadValue() => _slider.value.ToString("F3");

        public void Show(bool visible) => gameObject.SetActive(visible);

        // --- factory ---
        public static SliderWidget Instantiate(Transform parent, Item item, GameObject itemPrefab)
        {
            var go = Instantiate(itemPrefab, parent, worldPositionStays: false);
            go.name = "Item_" + item.id;
            var widget = go.GetComponentInChildren<SliderWidget>(includeInactive: true);
            if (widget == null)
            {
                Debug.LogError("SliderWidget: prefab missing SliderWidget component in children");
                Destroy(go);
                return null;
            }
            widget.Configure(item);
            return widget;
        }
    }
}

// Tiny extension
internal static class DictExt
{
    public static string TryGetOrDefault(this System.Collections.Generic.Dictionary<string, string> dict, string key, string defaultValue)
        => dict != null && dict.TryGetValue(key, out var v) ? v : defaultValue;
}
