using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealtimeDashboardUpdate3DPrinter : MonoBehaviour
{
    private EyeTracking eyeTracking = null;
    private RealtimeDashboard3DPrinter dashboardModel = null;
    private float valueExplorer = 0f;
    private float valueSolid = 0f;
    private float valuePlus = 0f;
    private float valuePro = 0f;

    void Start()
    {
        eyeTracking = GetComponent<EyeTracking>();
        dashboardModel = GameObject.Find("Dashboard").GetComponent<RealtimeDashboard3DPrinter>();
        // eyeTracking.OnGazeRecordProcessing += AnalyzeGazeRecord;
    }

    private void AnalyzeGazeRecord(GazeEventDetection.GazeRecord gazeRecord)
    {
        if (gazeRecord.gazeTarget.Contains("Explorer"))
        {
            valueExplorer += 0.005f;
            dashboardModel.SetValueExplorer(valueExplorer);
        }
        if (gazeRecord.gazeTarget.Contains("Solid"))
        {
            valueSolid += 0.005f;
            dashboardModel.SetValueSolid(valueSolid);
        }
        if (gazeRecord.gazeTarget.Contains("Plus"))
        {
            valuePlus += 0.005f;
            dashboardModel.SetValuePlus(valuePlus);
        }
        if (gazeRecord.gazeTarget.Contains("Pro"))
        {
            valuePro += 0.005f;
            dashboardModel.SetValuePro(valuePro);
        }
    }
}
