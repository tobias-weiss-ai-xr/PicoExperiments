using System.Collections.Generic;
using Unity.XR.PXR;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;

public unsafe class FtManager : MonoBehaviour
{
    public bool debug = true;
    public string url = "http://localhost:8001/save_to_csv";
    public SkinnedMeshRenderer leftEyeExample;
    public SkinnedMeshRenderer rightEyeExample;
    public SkinnedMeshRenderer head;
    public SkinnedMeshRenderer teethBlendShape;

    public GameObject text;
    public Transform TextParent;

    private List<TMP_Text> texts = new List<TMP_Text>();

    private List<string> blendShapeList = new List<string>
    {
        "eyeLookDownLeft",
        "noseSneerLeft",
        "eyeLookInLeft",
        "browInnerUp",
        "browDownRight",
        "mouthClose",
        "mouthLowerDownRight",
        "jawOpen",
        "mouthUpperUpRight",
        "mouthShrugUpper",
        "mouthFunnel",
        "eyeLookInRight",
        "eyeLookDownRight",
        "noseSneerRight",
        "mouthRollUpper",
        "jawRight",
        "browDownLeft",
        "mouthShrugLower",
        "mouthRollLower",
        "mouthSmileLeft",
        "mouthPressLeft",
        "mouthSmileRight",
        "mouthPressRight",
        "mouthDimpleRight",
        "mouthLeft",
        "jawForward",
        "eyeSquintLeft",
        "mouthFrownLeft",
        "eyeBlinkLeft",
        "cheekSquintLeft",
        "browOuterUpLeft",
        "eyeLookUpLeft",
        "jawLeft",
        "mouthStretchLeft",
        "mouthPucker",
        "eyeLookUpRight",
        "browOuterUpRight",
        "cheekSquintRight",
        "eyeBlinkRight",
        "mouthUpperUpLeft",
        "mouthFrownRight",
        "eyeSquintRight",
        "mouthStretchRight",
        "cheekPuff",
        "eyeLookOutLeft",
        "eyeLookOutRight",
        "eyeWideRight",
        "eyeWideLeft",
        "mouthRight",
        "mouthDimpleLeft",
        "mouthLowerDownLeft",
        "tongueOut",
        "viseme_PP",
        "viseme_CH",
        "viseme_o",
        "viseme_O",
        "viseme_i",
        "viseme_I",
        "viseme_RR",
        "viseme_XX",
        "viseme_aa",
        "viseme_FF",
        "viseme_u",
        "viseme_U",
        "viseme_TH",
        "viseme_kk",
        "viseme_SS",
        "viseme_e",
        "viseme_DD",
        "viseme_E",
        "viseme_nn",
        "viseme_sil",
    };

    private int[] indexList = new int[72];
    private float[] blendShapeWeights = new float[72];
    private int tongueIndex;
    private int leftLookDownIndex;
    private int leftLookUpIndex;
    private int leftLookInIndex;
    private int leftLookOutIndex;

    private int rightLookDownIndex;
    private int rightLookUpIndex;
    private int rightLookInIndex;
    private int rightLookOutIndex;

    private PxrFaceTrackingInfo faceTrackingInfo;
    private FileWriter fileWriter;

    void Start()
    {
        for (int i = 0; i < indexList.Length; i++)
        {
            indexList[i] = head.sharedMesh.GetBlendShapeIndex(blendShapeList[i]);
            GameObject textGO = Instantiate(text, TextParent);
            texts.Add(textGO.GetComponent<TMP_Text>());
        }

        tongueIndex = teethBlendShape.sharedMesh.GetBlendShapeIndex("tongueOut");
        leftLookDownIndex = leftEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookDownLeft");
        leftLookUpIndex = leftEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookUpLeft");
        leftLookInIndex = leftEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookInLeft");
        leftLookOutIndex = leftEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookOutLeft");
        rightLookDownIndex = rightEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookDownRight");
        rightLookUpIndex = rightEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookUpRight");
        rightLookInIndex = rightEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookInRight");
        rightLookOutIndex = rightEyeExample.sharedMesh.GetBlendShapeIndex("eyeLookOutRight");

        // Logging
        fileWriter = GetComponent<FileWriter>();
        fileWriter.columnNames = Enumerable.Range(0, 72).Select(x => x.ToString()).ToList();
        fileWriter.StartLogging();
    }

    // Update is called once per frame
    void Update()
    {
#if !UNITY_EDITOR
        if (PXR_Plugin.System.UPxr_QueryDeviceAbilities(PxrDeviceAbilities.PxrTrackingModeFaceBit))
        {
            switch (PXR_Manager.Instance.trackingMode)
            {
                case FaceTrackingMode.Hybrid:
                    PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_FACELIP_DATA, ref faceTrackingInfo);
                    break;
                case FaceTrackingMode.FaceOnly:
                    PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_FACE_DATA, ref faceTrackingInfo);
                    break;
                case FaceTrackingMode.LipsyncOnly:
                    PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_LIP_DATA, ref faceTrackingInfo);
                    break;
            }
            fixed (float* ptr = faceTrackingInfo.blendShapeWeight)
            {
                for (int i = 0; i < 72; ++i)
                {
                    texts[i].text = $"{blendShapeList[i]}\n{(int)(ptr[i] * 120)}";

                    if (indexList[i] >= 0)
                    {
                        head.SetBlendShapeWeight(indexList[i], ptr[i]);
                    }
                }

                teethBlendShape.SetBlendShapeWeight(tongueIndex, blendshapeWeights[51]);

                leftEyeExample.SetBlendShapeWeight(leftLookUpIndex, blendshapeWeights[31]);
                leftEyeExample.SetBlendShapeWeight(leftLookDownIndex, blendshapeWeights[0]);
                leftEyeExample.SetBlendShapeWeight(leftLookInIndex, blendshapeWeights[2]);
                leftEyeExample.SetBlendShapeWeight(leftLookOutIndex, blendshapeWeights[44]);
                rightEyeExample.SetBlendShapeWeight(rightLookUpIndex, blendshapeWeights[35]);
                rightEyeExample.SetBlendShapeWeight(rightLookDownIndex, blendshapeWeights[12]);
                rightEyeExample.SetBlendShapeWeight(rightLookInIndex, blendshapeWeights[11]);
                rightEyeExample.SetBlendShapeWeight(rightLookOutIndex, blendshapeWeights[45]);
            }
        }
#endif
    }
    public void SendValues(string state)
    {
        FtPayload p = new FtPayload(state, blendShapeWeights);
        StartCoroutine(ViaHttp(p));
        fileWriter.WriteLog(blendShapeWeights);
        if (debug) Debug.Log("sent values: " + state + " " + string.Join(" ", blendShapeWeights));
    }

    IEnumerator ViaHttp(FtPayload p)
    {
        if (p.State == null)
        {
            if (debug) Debug.Log("State missing");
            yield return 0;
        }
        if (p.Weights == null)
        {
            if (debug) Debug.Log("Weights missing");
            yield return 0;
        }


        var payload = JsonUtility.ToJson(p);

        using (UnityWebRequest www = UnityWebRequest.Post(url, payload, "application/json"))
        {
            www.SetRequestHeader("token", "asdf");
            www.SetRequestHeader("accept", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error + " " + payload);
            }
            else
            {
                if (debug) Debug.Log("Form upload complete!");
                if (debug) Debug.Log(www.downloadHandler.text);
            }
        }
    }
    public void ToggleDebugUI()
    {
        TextParent.gameObject.SetActive(!TextParent.gameObject.activeSelf);
        if (debug) Debug.Log("Toggle debug view");
    }
}

