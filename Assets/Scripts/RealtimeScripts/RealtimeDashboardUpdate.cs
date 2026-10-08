using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealtimeDashboardUpdate : MonoBehaviour
{
    private EyeTrackingManager eyeTracking = null;
    private RealtimeDashboard dashboardModel = null;
    private float valueAriel = 0f;
    private float valueKuschelweich = 0f;
    private float valueOmo = 0f;
    private float valueWeisserRiese = 0f;

    void Start()
    {
        eyeTracking = GetComponent<EyeTrackingManager>();
        if (eyeTracking == null && GameObject.Find("EyeTracking") != null)
            eyeTracking = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        dashboardModel = GameObject.Find("Dashboard")?.GetComponent<RealtimeDashboard>();

        if (eyeTracking != null && dashboardModel != null)
            eyeTracking.OnEyeTrackingEvent += AnalyzeGazeHit;
        else
            Debug.LogWarning("RealtimeDashboardUpdate: EyeTrackingManager or Dashboard not found; analysis disabled.");
    }

    // Runs per eye-tracking sample (24 Hz). The old per-record event never existed,
    // so analysis consumes the live gaze event directly.
    private void AnalyzeGazeHit(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        if (hit.transform == null)
            return;
        string gazeTarget = hit.transform.name;
        if (gazeTarget == "Ariel Products")
        {
            valueAriel += 0.005f;
            dashboardModel.SetValueAriel(valueAriel);
        }
        else if (gazeTarget == "Kuschelweich Products")
        {
            valueKuschelweich += 0.005f;
            dashboardModel.SetValueKuschelweich(valueKuschelweich);
        }
        else if (gazeTarget == "Omo Products")
        {
            valueOmo += 0.005f;
            dashboardModel.SetValueOmo(valueOmo);
        }
        else if (gazeTarget == "WeisserRiese Products")
        {
            valueWeisserRiese += 0.005f;
            dashboardModel.SetValueWeisserRiese(valueWeisserRiese);
        }
    }
}
