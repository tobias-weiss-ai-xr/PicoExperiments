using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class Main : DSingleton<Main>
{
    private Dictionary<int, string> avatar_id_dic = new Dictionary<int, string>();
    private bool menuIsDone;
    private bool triggerPressed;
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

        // //Agent Interaction
        // if (((InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed) || (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)) && SceneManager.GetSceneByBuildIndex(0) != SceneManager.GetActiveScene())
        // {
        //     // do something
        //     Debug.Log("Trigger pressed");
        // }

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
