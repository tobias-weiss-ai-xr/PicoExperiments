using System;
using UnityEngine;

namespace Convai.Scripts.Utils
{
    public class MicrophoneManager : MonoBehaviour
    {
        /// <summary>
        ///     Singleton instance of the MicrophoneTestController.
        /// </summary>
        public static MicrophoneManager Instance { get; private set; }

        /// <summary>
        ///     The UIMicrophoneSettings component attached to this GameObject.
        /// </summary>
        private UIMicrophoneSettings _uiMicrophoneSettings;

        /// <summary>
        ///     The currently selected microphone device.
        /// </summary>
        private string _selectedDevice;

        /// <summary>
        /// Event indicating that the selected Microphone has changed.
        /// </summary>
        public event Action<string> OnMicrophoneDeviceChanged;

        /// <summary>
        ///     Get the components on Awake.
        /// </summary>
        private void Awake()
        {
            // Ensure there's only one instance of MicrophoneTestController
            if (Instance != null)
            {
                Debug.Log("<color=red> There's More Than One MicrophoneTestController </color> " + transform + " - " +
                          Instance);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Get the components
            _uiMicrophoneSettings = GetComponent<UIMicrophoneSettings>();
        }

        private void Start()
        {
            _uiMicrophoneSettings.GetMicrophoneSelectDropdown().onValueChanged
                .AddListener(OnMicrophoneDropdownValueChanged);
        }

        /// <summary>
        ///     Called when the selected microphone device is changed.
        /// </summary>
        private void OnMicrophoneDropdownValueChanged(int selectedMicrophoneDeviceValue)
        {
            _selectedDevice = GetSelectedMicrophoneDevice();
            OnMicrophoneDeviceChanged?.Invoke(_selectedDevice);
        }

        /// <summary>
        ///     Get the selected microphone device from UI settings.
        /// </summary>
        public string GetSelectedMicrophoneDevice()
        {
            return _selectedDevice = _uiMicrophoneSettings.GetMicrophoneSelectDropdown()
                .options[_uiMicrophoneSettings.GetSelectedMicrophoneDeviceNumber()].text;
        }
    }
}