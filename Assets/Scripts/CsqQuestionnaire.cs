using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class CsqQuestionnaire : MonoBehaviour
{
    private GameObject defaultItem;
    private GameObject stageGo;
    GameObject activeItemGo;
    public class Stage
    {
        public string headerText;
        public string modalText;
        public string buttonOption;
        public string lowText;
        public string mediumText;
        public string highText;
    }
    int currentStage = 0;
    List<Stage> stageList = new List<Stage>() {
        new Stage() { headerText = "Nausea A", modalText = "Do you experience nausea (e.g., stomach pain, acid reflux, or tension to vomit)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "next"},
        new Stage() { headerText = "Nausea B", modalText = "Do you experience dizziness (e.g., light-headedness or spinning feeling)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "next"},
        new Stage() { headerText = "Vestibular A", modalText = "Do you experience disorientation (e.g., spatial confusion or vertigo)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "next"},
        new Stage() { headerText = "Vestibular B", modalText = "Do you experience postural instability (i.e., imbalance)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "next"},
        new Stage() { headerText = "Oculomotor A", modalText = "Do you experience a visually induced fatigue (e.g., feeling of tiredness or sleepiness)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "next"},
        new Stage() { headerText = "Oculomotor B", modalText = "Do you experience a visually induced discomfort (e.g., eyestrain, blurred vision, or headache)?", lowText="Absent Feeling", mediumText="Moderate Feeling", highText="Extreme Feeling", buttonOption = "exit"}
    };
    // Logging
    private bool logging = false;
    private static int logSize = 3;
    private string[] logData = new string[logSize];
    private StreamWriter writer = null;
    private static readonly string[] columnNames = {
        "headerText",
        "modalText",
        "rating",
    };
    private int flushCounter = 0;
    void Awake()
    {
        StartLogging();
    }
    void Start()
    {
        defaultItem = transform.Find("Item").gameObject;
        defaultItem.SetActive(false);
        NextStage();
    }
    void NextStage()
    {
        CreateStage(stageList[currentStage]);
        currentStage += 1;
    }

    void SaveStage(Stage stage)
    {
        logData[0] = stage.headerText;
        logData[1] = stage.modalText;
        logData[2] = stageGo.transform.Find("UI MinMaxSlider").GetComponent<Slider>().value.ToString();
        Log(logData);

        switch (stage.buttonOption)
        {
            case "next":
                NextStage();
                break;
            case "exit":
            default:
                Main.ChangeScene("00_Menu");
                break;
        }
    }

    void CreateStage(Stage stage)
    {
        // deactivate current
        if (activeItemGo) activeItemGo.SetActive(false);

        stageGo = Instantiate(defaultItem, defaultItem.transform.position, defaultItem.transform.rotation, transform);
        activeItemGo = stageGo;

        stageGo.transform.position = new Vector3(stageGo.transform.position.x, stageGo.transform.position.y, stageGo.transform.position.z);
        stageGo.SetActive(true);
        stageGo.transform.Find("Header Text").GetComponent<TMP_Text>().text = stage.headerText;
        stageGo.transform.Find("Modal Text").GetComponent<TMP_Text>().text = stage.modalText;
        stageGo.transform.Find("UI MinMaxSlider/Low Text").GetComponent<TMP_Text>(). text = stage.lowText;
        stageGo.transform.Find("UI MinMaxSlider/Medium Text").GetComponent<TMP_Text>(). text = stage.mediumText;
        stageGo.transform.Find("UI MinMaxSlider/High Text").GetComponent<TMP_Text>(). text = stage.highText;
        stageGo.transform.Find("Buttons/Submit").GetComponent<Button>().onClick.AddListener(() => SaveStage(stage));
    }

    private void OnDestroy()
    {
        StopLogging();
    }
    public void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Questionnaire Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

        string logPath = Application.persistentDataPath + "/Recordings/";
        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string fileName = string.Format("{0}-{1:00}-{2:00}-{3:00}h{4:00}m-{5}-questionnaire", now.Year, now.Month, now.Day, now.Hour, now.Minute, SceneManager.GetActiveScene().name);

        string path = logPath + fileName + ".csv";
        writer = new StreamWriter(path);

        Log(columnNames);
        Debug.Log("Raw Questionnaire Log file started at: " + path);
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
        Debug.Log("Raw Questionnaire Logging ended");
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


}
