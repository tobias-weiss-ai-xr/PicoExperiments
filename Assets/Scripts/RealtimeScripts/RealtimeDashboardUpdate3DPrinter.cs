using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealtimeDashboardUpdate3DPrinter : MonoBehaviour
{
    private EyeTrackingManager eyeTracking = null;
    private RealtimeDashboard3DPrinter dashboardModel = null;
    private float valueExplorer = 0f;
    private float valueSolid = 0f;
    private float valuePlus = 0f;
    private float valuePro = 0f;

    void Start()
    {
        eyeTracking = GetComponent<EyeTrackingManager>();
        if (eyeTracking == null && GameObject.Find("EyeTracking") != null)
            eyeTracking = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        dashboardModel = GameObject.Find("Dashboard")?.GetComponent<RealtimeDashboard3DPrinter>();

        if (eyeTracking != null && dashboardModel != null)
            eyeTracking.OnEyeTrackingEvent += AnalyzeGazeHit;
        else
            Debug.LogWarning("RealtimeDashboardUpdate3DPrinter: EyeTrackingManager or Dashboard not found; analysis disabled.");
    }

    // Runs per eye-tracking sample (24 Hz). The old per-record event never existed,
    // so analysis consumes the live gaze event directly.
    private void AnalyzeGazeHit(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        if (hit.transform == null)
            return;
        string gazeTarget = hit.transform.name;
        if (gazeTarget.Contains("Explorer"))
        {
            valueExplorer += 0.005f;
            dashboardModel.SetValueExplorer(valueExplorer);
        }
        else if (gazeTarget.Contains("Solid"))
        {
            valueSolid += 0.005f;
            dashboardModel.SetValueSolid(valueSolid);
        }
        else if (gazeTarget.Contains("Plus"))
        {
            valuePlus += 0.005f;
            dashboardModel.SetValuePlus(valuePlus);
        }
        else if (gazeTarget.Contains("Pro"))
        {
            valuePro += 0.005f;
            dashboardModel.SetValuePro(valuePro);
        }
    }
}
