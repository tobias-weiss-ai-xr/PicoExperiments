using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class FileWriter : MonoBehaviour
{
    public static int logSize = 3;
    public string folder = "/Recordings/";
    public string logname = "dummy";
    public List<string> columnNames = new();
    private bool logging = false;
    private string[] logData = new string[logSize];
    private string logPath = "";
    private string fileName = "";
    private string path = "";
    private StreamWriter writer = null;
    private int flushCounter = 0;
    void Awake()
    {
        // StartLogging();
    }
    private void OnDestroy()
    {
        StopLogging();
    }
    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        // create filename with timestamp
        DateTime now = DateTime.Now;
        fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}h{4:00}m-{5}-" + logname, now.Year, now.Month, now.Day, now.Hour, now.Minute, SceneManager.GetActiveScene().name);
        logPath = Application.persistentDataPath + folder;
        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
        path = logPath + fileName + ".csv";
        logging = true;
        writer = new StreamWriter(path);

        Debug.Log(string.Join(" ", columnNames));
        Debug.Log("Log file started at: " + path);
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
        Debug.Log("Logging ended at: " + path);
    }

    public void WriteLog(string[] values)
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
        if (flushCounter > 64)
        {
            writer.FlushAsync();
            flushCounter = 0;
        }
    }

    public void WriteLog(float[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        var arr = values.Select(x => x.ToString()).ToArray();
        for (int i = 0; i < arr.Length; ++i)
        {
            arr[i] = arr[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += arr[i] + (i == (arr.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);

        flushCounter += 1;
        if (flushCounter > 64)
        {
            writer.FlushAsync();
            flushCounter = 0;
        }
    }
}
