using Questionnaire;
using Questionnaire.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Questionnaire.UI
{
    /// <summary>
    /// MonoBehaviour that presents a survey in VR. Replaces CsqQuestionnaire.
    /// Drives SurveyRunner + ISurveyLogger + IQuestionWidget instances.
    /// </summary>
    public sealed class SurveySceneView : MonoBehaviour
    {
        [Header("Dialog UI")]
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private TMP_Text _modalText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextButtonLabel;

        [Header("Survey source")]
        [SerializeField] private ISurveyDataSource _surveySource;
        [SerializeField] private string _participantId = "";
        [SerializeField] private bool _autoStart = true;

        [Header("Navigation")]
        [SerializeField] private string _exitScene = "00_Menu";

        // Runtime state
        private Survey _survey;
        private SurveyRunner _runner;
        private CsvSurveyLogger _logger;
        private IQuestionWidget _currentWidget;

        private void Awake()
        {
            if (_itemPrefab == null)
                _itemPrefab = transform.Find("Item")?.gameObject;
            if (_itemContainer == null)
                _itemContainer = transform;
            if (_headerText == null)
                _headerText = transform.Find("Header Text")?.GetComponent<TMP_Text>();
            if (_modalText == null)
                _modalText = transform.Find("Modal Text")?.GetComponent<TMP_Text>();
            if (_nextButton == null)
                _nextButton = transform.Find("Buttons/Submit")?.GetComponent<Button>();
            if (_nextButtonLabel == null)
                _nextButtonLabel = _nextButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (_surveySource == null)
                _surveySource = GetComponent<ISurveyDataSource>();
        }

        private IEnumerator Start()
        {
            if (!_autoStart) yield break;

            yield return null; // let Awake / ser-des finish

            try
            {
                _survey = _surveySource.Load();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurveySceneView] failed to load survey: {ex.Message}");
                yield break;
            }

            _logger = new CsvSurveyLogger();
            var sceneName = SceneManager.GetActiveScene().name;
            _logger.Open(_participantId, sceneName, _survey.id);

            _runner = new SurveyRunner(_survey, _logger, () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _nextButton.onClick.AddListener(OnNextButton);

            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var item = _runner.CurrentItem;
            if (item == null) return;

            // Texts
            if (_headerText != null) _headerText.text = item.SectionHeaderFor(_survey);
            if (_modalText != null) _modalText.text = item.prompt;

            // Button label
            if (_nextButtonLabel != null)
                _nextButtonLabel.text = _runner.IsComplete ? "Exit" : (_runner.CurrentItem.ExitAfterSubmit ? "Exit" : "Next");

            // Widget: for now only slider type supported; auto-instantiate Item prefab
            var prefabName = "Item"; // legacy prefab is "Item" under this transform
            var prefab = _itemPrefab != null ? _itemPrefab : transform.Find("Item")?.gameObject;
            if (prefab != null && item.type == "slider")
            {
                // Re-use legacy CreateStage style: clone template, wire
                if (_currentWidget != null)
                    Destroy(_currentWidget as UnityEngine.Object);

                var goClone = Instantiate(prefab, _itemContainer);
                goClone.SetActive(true);
                // Wire slider texts
                var sliderGo = goClone.transform.Find("UI MinMaxSlider");
                if (sliderGo != null)
                {
                    var slider = sliderGo.GetComponent<Slider>();
                    if (slider != null) slider.value = 0.5f;
                    sliderGo.Find("Low Text")?.GetComponent<TMP_Text>()?.text = item.config?.TryGetOrDefault("lowText", "");
                    sliderGo.Find("Medium Text")?.GetComponent<TMP_Text>()?.text = item.config?.TryGetOrDefault("mediumText", "");
                    sliderGo.Find("High Text")?.GetComponent<TMP_Text>()?.text = item.config?.TryGetOrDefault("highText", "");
                }
            }
        }

        private void OnNextButton()
        {
            var item = _runner.CurrentItem;
            if (item == null) return;

            if (item.type == "slider" && _itemPrefab != null)
            {
                // Capture the slider value from the cloned Item UI
                var staged = _itemContainer?.Find("Item(Clone)");
                if (staged != null)
                {
                    var sliderGo = staged.Find("UI MinMaxSlider");
                    if (sliderGo != null && sliderGo.GetComponent<Slider>() is Slider s)
                    {
                        _currentWidget = new FakeSliderWidget(s.value);
                    }
                }
            }

            if (_runner.CanSubmit)
            {
                _runner.Submit();
            }

            if (_runner.IsComplete)
            {
                // Exit or final Submit
                if (string.IsNullOrEmpty(_exitScene))
                    _logger?.Close();
                else
                    SceneManager.LoadScene(_exitScene);
            }
            else
            {
                ShowCurrent();
            }
        }

        private void OnDestroy()
        {
            _logger?.Close();
            _runner?.Close();
        }

        // Temporary fake widget for legacy slider value capture (until SliderWidget MonoBehaviour is fully wired)
        private sealed class FakeSliderWidget : IQuestionWidget
        {
            private readonly float _value;
            public FakeSliderWidget(float value) => _value = value;
            public string Type => "slider";
            public bool IsComplete => true;
            public void Configure(Item item) { }
            public string ReadValue() => _value.ToString("F3");
            public void Show(bool visible) { }
        }
    }
}

// Extensions for finding section header
internal static class ItemExtensions
{
    public static string SectionHeaderFor(this Questionnaire.Item item, Questionnaire.Survey survey)
    {
        foreach (var sec in survey?.sections ?? new List<Questionnaire.Section>())
        foreach (var it in sec.items ?? new List<Questionnaire.Item>())
            if (it.id == item.id) return sec.header;
        return "";
    }
}
