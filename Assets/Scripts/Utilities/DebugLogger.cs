using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

public class DebugLogger : MonoBehaviour
{
    //public static DebugLogger Instance;
    private StreamWriter writer = null;

    private void Awake()
    {
        /*
        // Singleton instance
        // Only works for root objects
        // But we want to attach the Debug Object in Management
        // Design decision: seperate log for each scene
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        */

#if UNITY_EDITOR
        // Don't save in Assets folder when running in Unity player
        string logPath = Application.dataPath + "/../Recordings/";
#else
        string logPath = Application.dataPath + "/Recordings/";
#endif
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);
        System.DateTime now = System.DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}h{4:00}m-{5}-debug", now.Year, now.Month, now.Day, now.Hour, now.Minute, SceneManager.GetActiveScene().name);
        string path = logPath + fileName + ".csv";
        if (File.Exists(path))
            Debug.Log($"Continue log file at: {path}");
        else
            Debug.Log($"New log file at: {path}");
        writer  = new StreamWriter(path, true);
        Application.logMessageReceived += Log; // Subscribe
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        System.DateTime now = System.DateTime.Now;
        writer.WriteLine("" + now + ";" + logString);
    }
    void StopLogging()
    {
        Application.logMessageReceived -= Log; // Unsubscribe

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
        Debug.Log("Debug logging to file ended");
    }

    void OnDestroy()
    {
        StopLogging();
    }
}
