using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class EyeTrackingLogging : MonoBehaviour
{
    // Logging
    private bool logging = false;
    private static int logSize = 6;
    private string[] logData = new string[logSize];
    private StreamWriter writer = null;
    private static readonly string[] columnNames = {
        "DateTime",
        "HMDPosition",
        "GazeDirection",
        "HitPoint",
        "TargetPosition",
        "TargetName",
    };
    private int flushCounter = 0;

    void Awake()
    {
        EyeTrackingManager et = GetComponent<EyeTrackingManager>();
        StartLogging();
        // et.OnEyeTrackingEvent += EyeRay_OnEyeTrackingEvent;

    }
    private void EyeRay_OnEyeTrackingEvent(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        logData[0] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        logData[1] = origin.ToString();
        logData[2] = direction.ToString();
        logData[3] = hit.point.ToString();
        logData[4] = hit.transform == null ? "" : hit.transform.position.ToString();
        logData[5] = hit.transform == null ? "" : hit.transform.name.ToString();

        Log(logData);
    }

    private void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);

        flushCounter += 1;
        if (flushCounter > 10)
        {
            writer.FlushAsync();
            flushCounter = 0;
        }
    }

    private void OnDestroy()
    {
        StopLogging();
    }
    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("ET Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

        string logPath = Application.persistentDataPath + "/Recordings/";
        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}h{4:00}m-{5}-raw-et-data", now.Year, now.Month, now.Day, now.Hour, now.Minute, SceneManager.GetActiveScene().name);

        string path = logPath + fileName + ".csv";
        writer = new StreamWriter(path);

        Log(columnNames);
        Debug.Log("Raw ET Log file started at: " + path);
    }

    private void StopLogging()
    {
        if (!logging)
            return;

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
        logging = false;
        Debug.Log("Raw ET Logging ended");
    }
}
