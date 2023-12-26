using System.Collections;
using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;

public class IsControllerConnected : MonoBehaviour
{
    private GameObject leftHandController;
    private GameObject rightHandController;
    // Start is called before the first frame update
    void Start()
    {
        leftHandController = GameObject.Find("LeftHand Controller");
        rightHandController = GameObject.Find("RightHand Controller");

        InputDevices.deviceConnected += InputDevices_deviceConnected;
        InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
    }

    private void InputDevices_deviceDisconnected(InputDevice obj)
    {
        if(obj.name == "PicoXR Controller-Left")
        {
            leftHandController.SetActive(false);
        }
        if (obj.name == "PicoXR Controller-Right")
        {
            rightHandController.SetActive(false);
        }
    }
    private void InputDevices_deviceConnected(InputDevice obj)
    {
        if (obj.name == "PicoXR Controller-Left")
        {
            leftHandController.SetActive(true);
        }
        if (obj.name == "PicoXR Controller-Right")
        {
            rightHandController.SetActive(true);
        }
    }
    // Update is called once per frame
    void Update()
    {
    
    }
    private void OnDestroy()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
        InputDevices.deviceDisconnected -= InputDevices_deviceDisconnected;
    }
}
