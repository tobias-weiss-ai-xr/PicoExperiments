using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class Magnifier : MonoBehaviour
{

    private GameObject magnifierGo;
    private bool triggerActive;

    void Start()
    {
        magnifierGo = GameObject.Find("Magnifier");
        if (magnifierGo != null)
        {
            Debug.Log("Magnifier found and deactivated");
            magnifierGo.SetActive(false);
        }
    }
    void Update()
    {
        // Press Trigger
        if (((InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.triggerButton, out triggerActive) && triggerActive) || (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out triggerActive) && triggerActive)))
        {
            if (magnifierGo != null)
            {
                magnifierGo.SetActive(true);
                Debug.Log("Enable Magnifier");
            }
        }
        else
        {
            if (magnifierGo.activeInHierarchy)
            {
                magnifierGo.SetActive(false);
                Debug.Log("Disable Magnifier");
            }
        }
    }
}
