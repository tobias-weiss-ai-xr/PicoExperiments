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
    ///
    /// Auto-routes item.type string to specific widget prefabs.
    /// Prefab naming convention: "Widget_Slider", "Widget_Likert5", etc.
    /// 
    /// Auto-detection (legacy CsqQuestionnaire hierarchy):
    ///   - Item (GameObject template)
    ///   - Header Text (TMP_Text)
    ///   - Modal Text (TMP_Text)  
    ///   - UI MinMaxSlider/Slider (UnityEngine.UI.Slider)
    ///   - Buttons/Submit (Button)
    /// </summary>
    public sealed class SurveySceneView : MonoBehaviour
    {
        [Header("Dialog UI (auto-detected from children)")]
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private TMP_Text _modalText;
        [SerializeField] private Button _nextButton;
        [SerializeField] private TMP_Text _nextButtonLabel;

        [Header("Widget Prefabs (optional: override auto-loading from Resources)")]
        [SerializeField] private GameObject _sliderPrefab;
        [SerializeField] private GameObject _likert5Prefab;
        [SerializeField] private GameObject _yesNoPrefab;
        [SerializeField] private GameObject _singleChoicePrefab;
        [SerializeField] private GameObject _openTextPrefab;

        [Header("Survey Source")]
        [Tooltip("Direct JSON for the survey. If set, overrides surveyAsset.")]
        [SerializeField] private TextAsset _surveyAsset;
        [SerializeField] private string _surveyJsonText;
        [SerializeField] private string _participantId = "";
        [SerializeField] private bool _autoStart = true;

        [Header("Navigation")]
        [SerializeField] private string _exitScene = "00_Menu";
        [SerializeField] private bool _logDebug = true;
        [SerializeField] private bool _autoAdvanceOnCompleteWidget = true;

        // Runtime state
        private Survey _survey;
        private SurveyRunner _runner;
        private CsvSurveyLogger _logger;
        private GameObject _currentStageGo;
        private IQuestionWidget _currentWidget;
        private bool _closed;

        private void Awake()
        {
            // Auto-fill from hierarchy (legacy CsqQuestionnaire structure)
            if (_itemPrefab == null) _itemPrefab = transform.Find("Item")?.gameObject;
            if (_itemContainer == null) _itemContainer = transform;
            if (_headerText == null) _headerText = transform.Find("Header Text")?.GetComponent<TMP_Text>();
            if (_modalText == null) _modalText = transform.Find("Modal Text")?.GetComponent<TMP_Text>();
            if (_nextButton == null) _nextButton = transform.Find("Buttons/Submit")?.GetComponent<Button>();
            if (_nextButtonLabel == null) _nextButtonLabel = _nextButton?.GetComponentInChildren<TMP_Text>(includeInactive: true);

            if (_surveyAsset == null && !string.IsNullOrEmpty(_surveyJsonText))
            {
                _surveyAsset = new TextAsset { text = _surveyJsonText };
            }
        }

        private IEnumerator Start()
        {
            if (!_autoStart) yield break;
            yield return null; // Let Awake / serialization finish

            LoadAndStartSurvey();
        }

        public void LoadAndStartSurvey()
        {
            // Don't restart if already running
            if (_runner != null && !_runner.IsComplete) return;

            if (_surveyAsset == null)
            {
                Debug.LogError("[SurveySceneView] No survey asset assigned. Assign a TextAsset or set surveyJsonText.");
                return;
            }

            try
            {
                _survey = SurveySchema.Parse(_surveyAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurveySceneView] Failed to parse survey: {ex.Message}");
                return;
            }

            // Validate survey structure
            var validation = SurveyValidator.Validate(_survey);
            if (!validation.IsValid)
            {
                Debug.LogError($"[SurveySceneView] {validation.ToString()}");
                return;
            }
            SurveyValidator.ValidateOrThrow(_survey, w => Debug.LogWarning("[SurveySceneView] " + w));

            if (string.IsNullOrEmpty(_survey.id))
            {
                Debug.LogError("[SurveySceneView] Survey has no id.");
                return;
            }

            _closed = false;
            _logger = new CsvSurveyLogger();
            var sceneName = SceneManager.GetActiveScene().name;
            _logger.Open(_participantId, sceneName, _survey.id);

            _runner = new SurveyRunner(
                _survey,
                _logger,
                () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(OnNextButton);
            }

            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var item = _runner.CurrentItem;
            if (item == null)
            {
                HandleCompletion();
                return;
            }

            // Destroy previous stage
            if (_currentStageGo != null)
            {
                Destroy(_currentStageGo);
                _currentStageGo = null;
            }
            _currentWidget = null;

            // Header texts
            if (_headerText != null) _headerText.text = item.SectionHeaderFor(_survey);
            if (_modalText != null) _modalText.text = item.prompt;

            // Button label - use localized per-item exit preference
            bool shouldExit = _runner.IsComplete || (item != null && item.ExitAfterSubmit);
            if (_nextButtonLabel != null)
                _nextButtonLabel.text = shouldExit ? "Exit" : "Next";

            // Decide on widget type from item config
            _currentStageGo = InstantiateCorrectWidget(item);

            if (_currentStageGo != null)
                _currentStageGo.SetActive(true);

            UpdateSubmitButton();
        }

        private GameObject InstantiateCorrectWidget(Item item)
        {
            var type = item.type.ToLowerInvariant();
            GameObject prefab = null;

            // Try explicit assignments first
            switch (type)
            {
                case "slider":     prefab = _sliderPrefab ?? _itemPrefab; break;
                case "likert5":   prefab = _likert5Prefab; break;
                case "yesno":     prefab = _yesNoPrefab; break;
                case "singlechoice": prefab = _singleChoicePrefab; break;
                case "opentext":  prefab = _openTextPrefab; break;
            }

            // Fallback: try Resources loading with naming convention
            if (prefab == null)
            {
                var resourcePath = TryFindResourcePath(type);
                prefab = Resources.Load<GameObject>(resourcePath);
            }

            // Last fallback: legacy Item prefab for slider
            if (prefab == null && type == "slider" && _itemPrefab != null)
                prefab = _itemPrefab;

            if (prefab == null)
            {
                if (_logDebug)
                    Debug.LogWarning($"[SurveySceneView] No prefab found for item type '{type}'. Skipping widget.");
                return null;
            }

            var go = Instantiate(prefab, _itemContainer ?? transform);
            go.name = "Widget_" + item.id + "_" + type;

            // Get widget component and configure
            _currentWidget = go.GetComponent<IQuestionWidget>();
            if (go.transform.parent != null) go.transform.SetAsLastSibling();

            if (_currentWidget != null)
                _currentWidget.Configure(item);
            else if (type == "slider" && _logDebug)
                LegacySliderSetup(go, item);

            return go;
        }

        private string TryFindResourcePath(string type)
        {
            var candidates = new[]
            {
                "Widgets/Widget_" + type,
                "Widget_" + type,
                "Widgets/" + type,
                type
            };
            foreach (var path in candidates)
            {
                var prefab = Resources.Load<GameObject>(path);
                if (prefab != null) return path;
            }
            return null;
        }

        private void LegacySliderSetup(GameObject go, Item item)
        {
            var sliderGo = go.transform.Find("UI MinMaxSlider");
            if (sliderGo != null)
            {
                var slider = sliderGo.GetComponent<Slider>();
                if (slider != null) slider.value = 0.5f;
                sliderGo.Find("Low Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.Get("lowText", "Absent");
                sliderGo.Find("Medium Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.Get("mediumText", "Moderate");
                sliderGo.Find("High Text")?.GetComponent<TMP_Text>()?.text = 
                    item.config?.Get("highText", "Extreme");
            }
        }

        private void UpdateSubmitButton()
        {
            if (_nextButton == null) return;
            bool canSubmit = _currentWidget == null || _currentWidget.IsComplete;
            _nextButton.interactable = canSubmit && !_closed;
        }

        public void OnNextButton()
        {
            if (_closed) return;

            var item = _runner.CurrentItem;
            if (item == null)
            {
                HandleCompletion();
                return;
            }

            // Capture value from current widget
            try
            {
                if (_currentWidget != null)
                {
                    _runner.AttachWidget(_currentWidget);
                }
                // Legacy slider in Item prefab: capture from GameObject
                else if (_currentStageGo != null && item.type.ToLowerInvariant() == "slider")
                {
                    var sliderGo = _currentStageGo.transform.Find("UI MinMaxSlider");
                    var slider = sliderGo?.GetComponent<Slider>();
                    if (slider != null)
                    {
                        _runner.AttachWidget(new FakeSliderWidget(slider.value));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SurveySceneView] Failed to attach widget: {ex.Message}");
            }

            if (_runner.CanSubmit)
            {
                bool submitted = _runner.Submit();
                if (!submitted && _logDebug)
                    Debug.LogWarning("[SurveySceneView] Submit returned false");
            }
            else if (_logDebug)
            {
                Debug.LogWarning("[SurveySceneView] Cannot submit yet");
            }

            if (_runner.IsComplete)
            {
                HandleCompletion();
            }
            else
            {
                ShowCurrent();
            }
        }

        private void HandleCompletion()
        {
            if (_closed) return;
            _closed = true;

            _logger?.Close();
            _runner?.Close();

            if (_currentStageGo != null)
                Destroy(_currentStageGo);
            _currentStageGo = null;

            // Hide this view
            gameObject.SetActive(false);

            if (_logDebug)
                Debug.Log($"[SurveySceneView] Survey '{_survey?.id}' completed. Logging closed.");

            // Exit scene if configured
            if (!string.IsNullOrEmpty(_exitScene))
                SceneManager.LoadScene(_exitScene);
        }

        private void OnDestroy()
        {
            _logger?.Close();
            _runner?.Close();
        }

        // Allow external callers to trigger survey start
        public void StartSurvey() => LoadAndStartSurvey();

        public bool IsComplete => _closed || (_runner != null && _runner.IsComplete);

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
