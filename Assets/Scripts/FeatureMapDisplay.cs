using UnityEngine;
using TMPro;

public class FeatureMapDisplay : MonoBehaviour
{
    TMP_Text tmpText;
    FeatureMapRaycaster _raycaster;
    string _last;
    void Start()
    {
        tmpText = GameObject.Find("FeatureDisplay").GetComponent<TMP_Text>();
        _raycaster = GetComponent<FeatureMapRaycaster>();
    }

    void Update()
    {
        if (tmpText == null || _raycaster == null)
            return;
        // Committed area: feature-map mode shows the area label, whole-object
        // mode the object name. Same source the task manager and logging use.
        string a = _raycaster.CurrentArea;
        if (a != _last)
        {
            _last = a;
            tmpText.text = a;
        }
    }
}
