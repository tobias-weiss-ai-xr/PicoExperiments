using UnityEngine;
using UnityEngine.UI;

public class EyeTrackingInfo : MonoBehaviour
{
    public Text headerText;
    // Start is called before the first frame update
    void Start()
    {
        if (headerText == null) headerText = GameObject.Find("Gaze Info Header Text").GetComponent<Text>();
        EyeTracking et = GameObject.Find("EyeTracking").GetComponent<EyeTracking>();
        et.OnEyeTrackingEvent += SetInfoText;
    }

    private void SetInfoText(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        headerText.text = hit.transform == null ? "" : hit.transform.name;
    }
}
