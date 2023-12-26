using UnityEngine;
using System.IO;
// using Custom.Normal;
using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

public class Savefile : MonoBehaviour
{
    [Header("VR Settings")]
    public AvatarInput avatarInput;
    public AvatarLooks avatarModel;
    public GameObject xrOrigin;
    public GameObject gazeEventClassifier;


    [Header("Misc Settings")]
    public bool passiveDoorOpener;

    [Header("Optitrack Settings")]
    public GameObject optitrackManager;
    public GameObject xrInteractionManager;
    public GameObject RealtimeAvatarManager;
    public GameObject optitrackAvatar;
    public GameObject mustafaAvatar;
    public GameObject lilyAvatar;

    [Header("Third Person Settings")]
    public GameObject thirdpersonOrigin;
    public GameObject thirdpersonOriginRobot;
    public GameObject thirdpersonOriginTobias;
    public GameObject thirdpersonOriginAsmus;

    public static Savefile Instance;

    [System.Serializable]
    public class Savedata
    {
        public AvatarLooks avatarModel;
        public AvatarInput avatarInput;
        public string avatar;
        public bool passiveDoorOpener;
    }

    private void Awake()
    {
        Instance = this;
        this.EnsureObjectReference(ref optitrackManager, "OptitrackManager");
        this.EnsureObjectReference(ref xrInteractionManager, "XRInteractionManager");
        this.EnsureObjectReference(ref RealtimeAvatarManager, "Realtime Avatar Manager");
        this.EnsureObjectReference(ref xrOrigin, "XR Origin");
        this.EnsureObjectReference(ref thirdpersonOrigin, "Thirdperson Origin");
        this.EnsureObjectReference(ref thirdpersonOriginRobot, "Thirdperson Origin Robot");
        this.EnsureObjectReference(ref thirdpersonOriginTobias, "Thirdperson Origin Tobias");
        this.EnsureObjectReference(ref optitrackAvatar, "OptitrackAvatar");
        this.EnsureObjectReference(ref gazeEventClassifier, "GazeEventClassifier");
        Load();
        SelectComponents();
        
    }

    public void Save()
    {
        string path = Application.persistentDataPath + "/savefile.json";
        Savedata data = new Savedata();
        data.avatarModel = avatarModel;
        data.avatarInput = avatarInput;
        data.passiveDoorOpener = passiveDoorOpener;

        string json = JsonUtility.ToJson(data);

        File.WriteAllText(path, json);
        print($"Saving {path} successful");
    }

    public void Load()
    {
        string path = Application.persistentDataPath + "/savefile.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Savedata data = JsonUtility.FromJson<Savedata>(json);

            avatarModel = data.avatarModel;
            avatarInput = data.avatarInput;
            passiveDoorOpener = data.passiveDoorOpener;

            print($"Loading {path} successful");
        }
    }

    public void SelectComponents()
    {
        mustafaAvatar.SetActive(false);
        optitrackAvatar.SetActive(false);
        thirdpersonOrigin.SetActive(false);
        thirdpersonOriginRobot.SetActive(false);
        thirdpersonOriginTobias.SetActive(false);
        xrOrigin.SetActive(false);
        xrOrigin.GetComponent<EyeTracking>().enabled = false;
        xrOrigin.GetComponent<GazeEventDetection>().enabled = false;
        // xrOrigin.GetComponent<RealtimeDashboardUpdate3DPrinter>().enabled = false;
        // xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = false;
        // optitrackManager.GetComponent<OptitrackStreamingClient>().enabled = false;
        RealtimeAvatarManager.SetActive(false);
        if(avatarInput != AvatarInput.THIRDPERSON && avatarModel != AvatarLooks.BUSINESSMAN && avatarModel != AvatarLooks.MUSTAFA && avatarModel != AvatarLooks.LILY)
        {
            Debug.LogWarning("Invalid input/model combination. Switching model to businessman.");
            avatarModel = AvatarLooks.BUSINESSMAN;
        }


        switch (avatarInput)
        {
            // case AvatarInput.VIVE:
            //     xrOrigin.SetActive(true);
            //     // xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = true;
            //     break;
            // case AvatarInput.PICO:
            //     xrOrigin.SetActive(true);
            //     // xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = true; 
            //     break;
            // case AvatarInput.VARJO:
            //     xrOrigin.SetActive(true);
            //     RealtimeAvatarManager.SetActive(true);
            //     xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = true;
            //     xrOrigin.GetComponent<EyeTracking>().enabled = true;
            //     xrOrigin.GetComponent<GazeEventDetection>().enabled = true;
            //     xrOrigin.GetComponent<RealtimeDashboardUpdate3DPrinter>().enabled = true;
            //     break;
            // case AvatarInput.OPTITRACK:
            //     xrOrigin.SetActive(true);
            //     xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = true;
            //     optitrackManager.GetComponent<OptitrackStreamingClient>().enabled = true;
            //     if(avatarModel == AvatarLooks.MUSTAFA)
            //     {
            //         mustafaAvatar.SetActive(true);
            //     }
            //     else if(avatarModel == AvatarLooks.LILY)
            //     {
            //         lilyAvatar.SetActive(true);
            //     } else
            //     {
            //         optitrackAvatar.SetActive(true);
            //     }
            //     break;
            case AvatarInput.WEBCAM:
                break;
            case AvatarInput.THIRDPERSON:
                // xrInteractionManager.GetComponent<RuntimeXRLoaderManager>().enabled = false;
                break;
        }

        switch (avatarModel)
        {
            case AvatarLooks.ROBOT:
                thirdpersonOriginRobot.SetActive(true);
                break;
            case AvatarLooks.BUSINESSMAN:
                if (avatarInput == AvatarInput.THIRDPERSON)
                {
                    thirdpersonOrigin.SetActive(true);
                }             
                //Optitrack Handled in previous switch
                break;
            case AvatarLooks.ASMUS:
                thirdpersonOriginAsmus.SetActive(true);
                break;
            case AvatarLooks.TOBIAS:
                thirdpersonOriginTobias.SetActive(true);
                break;
            case AvatarLooks.MUSTAFA:
                //Handled in previous switch
                break;
            case AvatarLooks.LILY:
                lilyAvatar.SetActive(true); break;
        }

        // Passive Door Opener
        if (!passiveDoorOpener)
        {
            // gazeEventClassifier.GetComponent<OpenDoorPassive>().enabled = false;
            Debug.Log("Disabled Passive Door Opener");
        }
    }
}
public enum AvatarInput
{
    VIVE, VARJO, PICO, OPTITRACK, WEBCAM, THIRDPERSON
}

public enum AvatarLooks
{
    TOBIAS, ASMUS, BUSINESSMAN, ROBOT, MUSTAFA, LILY
}
