using Questionnaire;
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
    /// Auto-routes item.type string to specific widget prefabs (SliderWidget, etc.)
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

        [Header("Widget routing (optional: override per type)")]
        [SerializeField] private GameObject _sliderPrefab;
        [SerializeField] private GameObject _likert5Prefab;
        [SerializeField] private GameObject _yesNoPrefab;
        [SerializeField] private GameObject _singleChoicePrefab;
        [SerializeField] private GameObject _openTextPrefab;

        [Header("Survey source")]
        [SerializeField] private ISurveyDataSource _surveySource;
        [SerializeField] private string _participantId = "";
        [SerializeField] private bool _autoStart = true;

        [Header("Navigation")]
        [SerializeField] private string _exitScene = "00_Menu";
        [SerializeField] private bool _logDebug = true;

        // Runtime state
        private Survey _survey;
        private SurveyRunner _runner;
        private CsvSurveyLogger _logger;
        private GameObject _currentStageGo;
        private IQuestionWidget _currentWidget;

        private void Awake()
        {
            // Auto-fill from hierarchy (legacy CsqQuestionnaire structure)
            if (_itemPrefab == null) _itemPrefab = transform.Find("Item")?.gameObject;
            if (_itemContainer == null) _itemContainer = transform;
            if (_headerText == null) _headerText = transform.Find("Header Text")?.GetComponent<TMP_Text>();
            if (_modalText == null) _modalText = transform.Find("Modal Text")?.GetComponent<TMP_Text>();
            if (_nextButton == null) _nextButton = transform.Find("Buttons/Submit")?.GetComponent<Button>();
            if (_nextButtonLabel == null) _nextButtonLabel = _nextButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (_surveySource == null) _surveySource = GetComponent<ISurveyDataSource>();
        }

        private IEnumerator Start()
        {
            if (!_autoStart) yield break;
            yield return null; // Let Awake / serialization finish

            try
            {
                _survey = _surveySource?.Load() ?? throw new InvalidOperationException("SurveySource is required");
                if (string.IsNullOrEmpty(_survey.id))
                    throw new InvalidOperationException("Survey loaded but has no id");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurveySceneView] Failed to load survey: {ex.Message}");
                yield break;
            }

            InitializeRunner();
        }

        private void InitializeRunner()
        {
            _logger = new CsvSurveyLogger();
            var sceneName = SceneManager.GetActiveScene().name;
            _logger.Open(_participantId, sceneName, _survey.id);

            _runner = new SurveyRunner(
                _survey,
                _logger,
                () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            _nextButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(OnNextButton);

            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var item = _runner.CurrentItem;
            if (item == null) return;

            // Destroy previous stage
            if (_currentStageGo != null) Destroy(_currentStageGo);
            _currentStageGo = null;
            _currentWidget = null;

            // Header texts
            if (_headerText != null) _headerText.text = item.SectionHeaderFor(_survey);
            if (_modalText != null) _modalText.text = item.prompt;

            // Button label
            if (_nextButtonLabel != null)
                _nextButtonLabel.text = _runner.IsComplete ? "Exit" : (
                    _runner.CurrentItem.ExitAfterSubmit ? "Exit" : "Next");

            // Instantiate item UI based on type
            var prefab = SelectPrefabFor(item.type);
            if (prefab != null)
            {
                _currentStageGo = Instantiate(prefab, _itemContainer);
                _currentWidget = _currentStageGo.GetComponent<IQuestionWidget>();
                if (_currentWidget != null)
                    _currentWidget.Configure(item);
            }

            // Fallback: legacy slider via CsqQuestionnaire hierarchy
            if (_currentWidget == null && item.type == "slider")
            {
                LegacySliderSetup(item);
            }

            if (_currentStageGo != null)
                _currentStageGo.SetActive(true);

            // Auto-mark not complete if widget incomplete
            _nextButton.interactable = _currentWidget == null || _currentWidget.IsComplete;
        }

        private GameObject SelectPrefabFor(string type)
        {
            return type switch
            {
                "slider"     => _sliderPrefab ?? _itemPrefab,
                "likert5"   => _likert5Prefab,
                "yesNo"     => _yesNoPrefab,
                "singleChoice" => _singleChoicePrefab,
                "openText"  => _openTextPrefab,
                _           => null
            };
        }

        private void LegacySliderSetup(Item item)
        {
            if (_itemPrefab == null) return;

            _currentStageGo = Instantiate(_itemPrefab, _itemContainer);
            var sliderGo = _currentStageGo.transform.Find("UI MinMaxSlider");
            if (sliderGo != null)
            {
                var slider = sliderGo.GetComponent<Slider>();
                if (slider != null) slider.value = 0.5f;
                sliderGo.Find("Low Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.TryGetOrDefault("lowText", "Absent Feeling");
                sliderGo.Find("Medium Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.TryGetOrDefault("mediumText", "Moderate Feeling");
                sliderGo.Find("High Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.TryGetOrDefault("highText", "Extreme Feeling");
            }
            _currentWidget = new FakeSliderWidget(); // Will be populated in OnNext
        }

        private void OnNextButton()
        {
            var item = _runner.CurrentItem;
            if (item == null || !item.ExitAfterSubmit) return;

            // Capture value from current UI
            if (_currentWidget != null)
            {
                _runner.AttachWidget(_currentWidget);
            }
            // Legacy slider: capture from instantiated GameObject
            else if (_currentStageGo != null && item.type == "slider")
            {
                var sliderGo = _currentStageGo.transform.Find("UI MinMaxSlider");
                var slider = sliderGo?.GetComponent<Slider>();
                if (slider != null)
                {
                    _runner.AttachWidget(new FakeSliderWidget(slider.value));
                }
            }

            if (_runner.CanSubmit || _runner.IsComplete)
            {
                _runner.Submit();
            }
            else if (_logDebug)
            {
                Debug.LogWarning("[SurveySceneView] Cannot submit - item not complete");
                return;
            }

            if (_runner.IsComplete)
            {
                FinalizeAndExit();
            }
            else
            {
                ShowCurrent();
            }
        }

        private void FinalizeAndExit()
        {
            _logger?.Close();
            _runner?.Close();

            if (string.IsNullOrEmpty(_exitScene))
            {
                _currentStageGo?.SetActive(false);
                gameObject.SetActive(false);
            }
            else
            {
                SceneManager.LoadScene(_exitScene);
            }
        }

        private void OnDestroy()
        {
            _logger?.Close();
            _runner?.Close();
        }

        // --- Helper types ---

        private sealed class FakeSliderWidget : IQuestionWidget
        {
            private readonly float _value;
            public FakeSliderWidget(float value = 0.5f) => _value = value;
            public string Type => "slider";
            public bool IsComplete => true;
            public void Configure(Item item) { }
            public string ReadValue() => _value.ToString("F3");
            public void Show(bool visible) { }
        }
    }
}
