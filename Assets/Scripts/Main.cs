using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class Main : DSingleton<Main>
{
    private Dictionary<int, string> avatar_id_dic = new Dictionary<int, string>();
    private bool menuIsDone;
    public bool AdaptiveAgent;
    public static bool XrKeydownIndexBool = true;

    public List<Vector3> EyeTrackingDirectionAdjustments;
    [System.Serializable]
    public class EyeTrackingDirectionAdjustmentsSavedata
    {
        public List<Vector3> eyeTrackingDirectionAdjustments;
    }

    private GameObject leftHandController;
    private GameObject rightHandController;

    float delaytime;
    protected override void Awake()
    {
        base.Awake();
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "00_Menu")
        {
            // AdaptiveAgent = Random.value > 0.5f;
            Toggle t = GameObject.Find("AdaptiveAgentToggle").GetComponent<Toggle>();
            AdaptiveAgent = false;
            t.onValueChanged.AddListener((val) => AdaptiveAgent = val);
            t.onValueChanged.AddListener(LogAiAgentMode);
        }
        else if (currentSceneName == "00_Menu_full")
        {
            Toggle t = GameObject.Find("AdaptiveAgentToggle").GetComponent<Toggle>();
            AdaptiveAgent = false;
            t.onValueChanged.AddListener((val) => AdaptiveAgent = val);
        }
    }

    void LogAiAgentMode(bool mode)
    {
        string filePath = Application.persistentDataPath + "/agent-status.txt";
        string dt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Debug.Log(dt + " Adaptive agent: " + AdaptiveAgent.ToString());
        using (StreamWriter writer = File.AppendText(filePath))
        {
            writer.WriteLine(dt + " Adaptive agent: " + AdaptiveAgent.ToString());
        }
    }

    void Update()
    {
        //Long Press Menu Button
        if (((Input.GetKey(KeyCode.Escape) || (InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.menuButton, out menuIsDone) && menuIsDone) || (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.menuButton, out menuIsDone) && menuIsDone))) && SceneManager.GetSceneByBuildIndex(0) != SceneManager.GetActiveScene())
        {
            delaytime += Time.deltaTime;
            if (delaytime >= 2)
            {
                delaytime = 0;
                if (SceneManager.GetActiveScene().name == "00_Menu")
                    ChangeScene("Calibration");
                else
                    ChangeScene(0);
            }
        }
    }
    public static void ChangeScene(string sceneName)
    {
        Debug.Log($"Changing scene to {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    public static void ChangeScene(int sceneId)
    {
        Debug.Log($"Changing scene to {sceneId.ToString()}");
        SceneManager.LoadScene(sceneId, LoadSceneMode.Single);
    }
    public void SaveEyetrackingCalibration(string savefile = "et_calibration_settings.json")
    {
        string configPath = Application.persistentDataPath + "/Config/";
        if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);
        string path = configPath + savefile;

        Main.EyeTrackingDirectionAdjustmentsSavedata data = new Main.EyeTrackingDirectionAdjustmentsSavedata();
        data.eyeTrackingDirectionAdjustments = Main.Instance.EyeTrackingDirectionAdjustments;

        string json = JsonUtility.ToJson(data);

        try
        {
            File.WriteAllText(path, json);
            ChangeStatusMessage($"Saving calibration data successful");
        }
        catch (System.Exception e)
        {
            ChangeStatusMessage($"Saving calibration data error {e}");
        }
    }
    public void ChangeStatusMessage(string statusMsg)
    {
        print(statusMsg);
    }

    public void TestStatus() => ChangeStatusMessage("Test");
}
