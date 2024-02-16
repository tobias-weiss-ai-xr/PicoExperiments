using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.XR;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using DG.Tweening;

public class EyeTrackingManager : MonoBehaviour
{
    public Transform Origin;
    public GameObject SpotLight;
    public Transform gazePoint;
    private Vector3 combineEyeGazeVector;
    private Vector3 combineEyeGazeOriginOffset;
    private Vector3 combineEyeGazeOrigin;
    private Matrix4x4 headPoseMatrix;
    private Matrix4x4 originPoseMatrix;

    private Vector3 combineEyeGazeVectorInWorldSpace;
    private Vector3 combineEyeGazeOriginInWorldSpace;

    private Vector2 primary2DAxis;

    private RaycastHit hitinfo;

    private Transform selectedObj;

    private bool wasPressed;
    TrackingStateCode trackingState;
    private bool supported = false;

    public bool DebugLog = true;
    Matrix4x4 matrix;
    // Logging
    public event EyeTrackingEvent OnEyeTrackingEvent;
    public delegate void EyeTrackingEvent(Vector3 origin, Vector3 direction, RaycastHit hit);

    void Start()
    {
        gazePoint = GameObject.Find("gazePoint").transform;
        combineEyeGazeOriginOffset = Vector3.zero;
        combineEyeGazeVector = Vector3.zero;
        combineEyeGazeOrigin = Vector3.zero;
        originPoseMatrix = Origin.localToWorldMatrix;
        trackingState = (TrackingStateCode)PXR_MotionTracking.WantEyeTrackingService();
        // Query if the current device supports eye tracking
        EyeTrackingMode eyeTrackingMode = EyeTrackingMode.PXR_ETM_NONE;
        int supportedModesCount = 0;
        trackingState = (TrackingStateCode)PXR_MotionTracking.GetEyeTrackingSupported(ref supported, ref supportedModesCount, ref eyeTrackingMode);
    }

    void Update()
    {
        if (Camera.main)
        {
            matrix = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
        }
        else
        {
            matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
        }
        bool result = (PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 Origin) && PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 Direction));
        PXR_EyeTracking.GetCombineEyeGazePoint(out Origin);
        PXR_EyeTracking.GetCombineEyeGazeVector(out Direction);
        var OriginOffset = matrix.MultiplyPoint(Origin);
        var DirectionOffset = matrix.MultiplyVector(Direction);
        if (result)
        {
            Ray ray = new Ray(OriginOffset, DirectionOffset);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200))
            {
                gazePoint.gameObject.SetActive(true);
                gazePoint.DOMove(hit.point, Time.deltaTime).SetEase(Ease.Linear);
            }
            else
            {
                gazePoint.gameObject.SetActive(false);
            }
            // Invoke logging event
            OnEyeTrackingEvent?.Invoke(OriginOffset, DirectionOffset, hit);
        }
    }

    void UpdateNew()
    {
        if (supported)
        {

            // //Offest Adjustment
            // if (InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out primary2DAxis))
            // {

            //     combineEyeGazeOriginOffset.x += primary2DAxis.x * 0.001f;
            //     combineEyeGazeOriginOffset.y += primary2DAxis.y * 0.001f;

            // }

            PXR_EyeTracking.GetHeadPosMatrix(out headPoseMatrix);

            bool isSuccess = PXR_EyeTracking.GetCombineEyeGazeVector(out combineEyeGazeVector);
            if (!isSuccess) return;

            PXR_EyeTracking.GetCombineEyeGazePoint(out combineEyeGazeOrigin);
            //Translate Eye Gaze point and vector to world space
            combineEyeGazeOrigin += combineEyeGazeOriginOffset;
            combineEyeGazeOriginInWorldSpace = originPoseMatrix.MultiplyPoint(headPoseMatrix.MultiplyPoint(combineEyeGazeOrigin));
            combineEyeGazeVectorInWorldSpace = originPoseMatrix.MultiplyVector(headPoseMatrix.MultiplyVector(combineEyeGazeVector));

            if (SpotLight != null)
            {
                SpotLight.transform.position = combineEyeGazeOriginInWorldSpace;
                SpotLight.transform.rotation = Quaternion.LookRotation(combineEyeGazeVectorInWorldSpace, Vector3.up);
            }

            GazeTargetControl(combineEyeGazeOriginInWorldSpace, combineEyeGazeVectorInWorldSpace);
        }
        else
        {
            if (DebugLog) Debug.Log("TW: Eye tracking not supported");
            GazeTargetControl(Camera.main.transform.position, Camera.main.transform.forward);
        }

    }


    void GazeTargetControl(Vector3 origin, Vector3 vector)
    {
        if (Physics.SphereCast(origin, 0.15f, vector, out hitinfo))
        {

            OnEyeTrackingEvent?.Invoke(combineEyeGazeOriginInWorldSpace, combineEyeGazeVectorInWorldSpace, hitinfo);

            if (gazePoint != null)
            {
                gazePoint.gameObject.SetActive(true);
                gazePoint.transform.position = hitinfo.point;
            }
            else
            {
                gazePoint.gameObject.SetActive(false);
            }

            if (DebugLog)
            {
                Debug.Log($"TW: gaze dot position {origin}\n name {hitinfo.transform.name}\n TW: hit distance {hitinfo.distance.ToString()} \n TW: xr origin {hitinfo.transform.position}");
            }

            if (selectedObj != null && selectedObj != hitinfo.transform)
            {
                if (selectedObj.GetComponent<ETObject>() != null)
                    selectedObj.GetComponent<ETObject>().UnFocused();
                selectedObj = null;
            }
            else if (selectedObj == null)
            {
                selectedObj = hitinfo.transform;
                if (selectedObj.GetComponent<ETObject>() != null)
                    selectedObj.GetComponent<ETObject>().IsFocused();
            }

        }
        else
        {
            if (DebugLog) Debug.Log("TW: No hit");
            if (selectedObj != null)
            {
                if (selectedObj.GetComponent<ETObject>() != null)
                    selectedObj.GetComponent<ETObject>().UnFocused();
                selectedObj = null;
            }
        }
    }
}
