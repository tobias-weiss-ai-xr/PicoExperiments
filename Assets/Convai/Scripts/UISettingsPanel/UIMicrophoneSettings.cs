using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     This class is used to manage the microphone settings in the UI.
/// </summary>
/// 
namespace Convai.Scripts.Utils
{
    public class UIMicrophoneSettings : MonoBehaviour
    {
        [Tooltip("Dropdown to select the microphone device to use.")]
        [SerializeField]
        private TMP_Dropdown _microphoneSelectDropdown;

        [Tooltip("Button to control the recording.")]
        [SerializeField]
        private Button _recordControllerButton;

        [Tooltip("Text to display the status of the recording system.")]
        [SerializeField]
        private TextMeshProUGUI _recordSystemStatusText;

        /// <summary>
        ///     Image component of the recording control button.
        /// </summary>
        private Image _buttonImage;

        /// <summary>
        ///     Text component of the recording control button.
        /// </summary>
        private TextMeshProUGUI _buttonText;

        /// <summary>
        ///     Reference to the MicrophoneManager to subscribe to its events.
        /// </summary>
        private MicrophoneTestController _microphoneTestController;

        /// <summary>
        ///     Index of the selected microphone device.
        /// </summary>
        private int _selectedMicrophoneDeviceNumber;

        /// <summary>
        ///     Initialize references to other scripts and components and populate the dropdown with available microphone devices.
        /// </summary>
        private void Awake()
        {
            _microphoneTestController = GetComponent<MicrophoneTestController>();
            _buttonImage = _recordControllerButton.GetComponent<Image>();
            _buttonText = _recordControllerButton.GetComponentInChildren<TextMeshProUGUI>();
            _microphoneSelectDropdown.ClearOptions();
            _microphoneSelectDropdown.AddOptions(new List<string>(Microphone.devices));
            _microphoneSelectDropdown.onValueChanged.AddListener(ChangeSelectedDevice);
            _recordSystemStatusText.text = "Waiting For Record...";
        }

        /// <summary>
        ///     Subscribe to events when this component is enabled.
        /// </summary>
        private void OnEnable()
        {
            _microphoneTestController.OnRecordStarted += MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted += MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted += MicrophoneTestControllerOnAudioClipCompleted;
            UISaveLoadSystem.Instance.OnLoad += UISaveLoadSystem_OnLoad;
            UISaveLoadSystem.Instance.OnSave += UISaveLoadSystem_OnSave;
        }

        /// <summary>
        ///     Unsubscribe from events when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            _microphoneTestController.OnRecordStarted -= MicrophoneTestControllerOnRecordStarted;
            _microphoneTestController.OnRecordCompleted -= MicrophoneTestControllerOnRecordCompleted;
            _microphoneTestController.OnAudioClipCompleted -= MicrophoneTestControllerOnAudioClipCompleted;
            UISaveLoadSystem.Instance.OnLoad -= UISaveLoadSystem_OnLoad;
            UISaveLoadSystem.Instance.OnSave -= UISaveLoadSystem_OnSave;
        }

        /// <summary>
        ///     Event handler when the selected microphone device is changed.
        /// </summary>
        private void ChangeSelectedDevice(int selectedDeviceNumber)
        {
            _selectedMicrophoneDeviceNumber = selectedDeviceNumber;
            UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber = _selectedMicrophoneDeviceNumber;
            Logger.Info("Microphone Device Updated.", Logger.LogCategory.Character);
        }

        /// <summary>
        ///     Event handler when saved data is loaded.
        /// </summary>
        private void UISaveLoadSystem_OnLoad()
        {
            _microphoneSelectDropdown.value = UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber;
            Logger.Info("Loaded Microphone Device. ", Logger.LogCategory.Character);
        }

        /// <summary>
        ///     Event handler when data is saved.
        /// </summary>
        private void UISaveLoadSystem_OnSave()
        {
            UISaveLoadSystem.Instance.SelectedMicrophoneDeviceNumber = _microphoneSelectDropdown.value;
        }

        /// <summary>
        ///     Event handler when a recording is started.
        /// </summary>
        private void MicrophoneTestControllerOnRecordStarted()
        {
            _recordSystemStatusText.text = "Recording...";
            _buttonImage.color = Color.red;
            _buttonText.text = "Stop";
            _buttonText.color = new Color(1, 1, 1);
        }

        /// <summary>
        ///     Event handler when a recording is completed.
        /// </summary>
        private void MicrophoneTestControllerOnRecordCompleted()
        {
            _recordSystemStatusText.text = "Playing...";
            _buttonImage.color = Color.green;
            _buttonText.text = "Rec";
            _buttonText.color = new Color(0.14f, 0.14f, 0.14f);
        }

        /// <summary>
        ///     Event handler when the audio clip playback is completed.
        /// </summary>
        private void MicrophoneTestControllerOnAudioClipCompleted()
        {
            _recordSystemStatusText.text = "Waiting For Record...";
        }

        /// <summary>
        ///     Returns the microphone selection dropdown object.
        /// </summary>
        public TMP_Dropdown GetMicrophoneSelectDropdown()
        {
            return _microphoneSelectDropdown;
        }

        /// <summary>
        ///     Returns the record control button object.
        /// </summary>
        public Button GetRecordControllerButton()
        {
            return _recordControllerButton;
        }

        /// <summary>
        ///     Returns the selected microphone device number.
        /// </summary>
        public int GetSelectedMicrophoneDeviceNumber()
        {
            return _selectedMicrophoneDeviceNumber;
        }
    }
}