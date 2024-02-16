using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EyeTrackingAppearanceRule : MonoBehaviour
{
    public enum Direction
    {
    }
    Dictionary<string, bool> PrinterDict = new() {
        {"3D WS Explorer", false},
        {"3D WS Solid", false},
        {"3D WS Plus", false},
        {"3D WS Pro", false},
    };
    public Text headerText;
    // Start is called before the first frame update
    void Start()
    {
        EyeTrackingManager et = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        // et.OnEyeTrackingEvent += ProcessAppearanceRule;
    }

    private void ProcessAppearanceRule(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        string key = hit.transform.name.ToString();
        PrinterDict[key] = true;
        bool anyFalse = PrinterDict.Any(KeyValuePair => !KeyValuePair.Value);
        if (!anyFalse)
        {
            StartCoroutine(GetComponent<NavMeshAgentTarget>().MoveToConsumer(0f));
        }
    }
}
