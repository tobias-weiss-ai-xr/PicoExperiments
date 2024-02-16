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
        dashboardModel = GameObject.Find("Dashboard").GetComponent<RealtimeDashboard>();
        // eyeTracking.OnGazeRecordProcessing += AnalyzeGazeRecord;
    }

    private void AnalyzeGazeRecord(GazeEventDetection.GazeRecord gazeRecord)
    {
        if (gazeRecord.gazeTarget == "Ariel Products")
        {
            valueAriel += 0.005f;
            dashboardModel.SetValueAriel(valueAriel);
        }
        if (gazeRecord.gazeTarget == "Kuschelweich Products")
        {
            valueKuschelweich += 0.005f;
            dashboardModel.SetValueKuschelweich(valueKuschelweich);
        }
        if (gazeRecord.gazeTarget == "Omo Products")
        {
            valueOmo += 0.005f;
            dashboardModel.SetValueOmo(valueOmo);
        }
        if (gazeRecord.gazeTarget == "WeisserRiese Products")
        {
            valueWeisserRiese += 0.005f;
            dashboardModel.SetValueWeisserRiese(valueWeisserRiese);
        }
    }
}
