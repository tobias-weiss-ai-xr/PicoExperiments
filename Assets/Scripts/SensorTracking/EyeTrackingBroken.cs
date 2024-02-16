using System;
using System.Collections;
using UnityEngine;
using Unity.XR.PXR;

public class EyeTrackingBroken : MonoBehaviour
{
    // Positions and targets
    private float maxDistance = 100.0f;
    private Transform gazePoint;
    private Transform xrOrigin;
    public bool showGazePoint = true;
    public bool DebugLog = true;

    // Internals
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

    // Logging
    public event EyeTrackingEvent OnEyeTrackingEvent;
    public delegate void EyeTrackingEvent(Vector3 origin, Vector3 direction, RaycastHit hit);
    public Transform Origin;
    TrackingStateCode trackingState;
    private bool supported = false;
    int layerMask;

    void Start()
    {
        // Everything except layer 2 (Raycast Ignore)
        int layer = 2;
        layerMask = 1 << layer;
        layerMask = ~layerMask;
        gazePoint = GameObject.Find("gazePoint").transform;
        xrOrigin = GameObject.Find("XR Origin").transform;

        combineEyeGazeOriginOffset = Vector3.zero;
        combineEyeGazeVector = Vector3.zero;
        combineEyeGazeOrigin = Vector3.zero;
        originPoseMatrix = Origin.localToWorldMatrix;
        trackingState = (TrackingStateCode)PXR_MotionTracking.WantEyeTrackingService();

        if (DebugLog) Debug.Log(trackingState.ToString());

        // Query if the current device supports eye tracking
        EyeTrackingMode eyeTrackingMode = EyeTrackingMode.PXR_ETM_NONE;
        if (DebugLog) Debug.Log(eyeTrackingMode.ToString());
        int supportedModesCount = 0;
        trackingState = (TrackingStateCode)PXR_MotionTracking.GetEyeTrackingSupported(ref supported, ref supportedModesCount, ref eyeTrackingMode);

        StartCoroutine(EyeRaycast(0.04f)); // default: 0.04 sec. = 24 FPS
    }

    IEnumerator EyeRaycast(float steptime)
    {
        while (true)
        {
            PXR_EyeTracking.GetHeadPosMatrix(out headPoseMatrix);

            bool isSuccess = PXR_EyeTracking.GetCombineEyeGazeVector(out combineEyeGazeVector);
            if (!isSuccess)
            {
                if (DebugLog)
                    Debug.Log("Failed to get eye gaze vector");
                yield return new WaitForSeconds(steptime);
            }

            PXR_EyeTracking.GetCombineEyeGazePoint(out combineEyeGazeOrigin);
            if (DebugLog) Debug.Log(combineEyeGazeOrigin.ToString());
            //Translate Eye Gaze point and vector to world space
            combineEyeGazeOrigin += combineEyeGazeOriginOffset;
            combineEyeGazeOriginInWorldSpace = originPoseMatrix.MultiplyPoint(headPoseMatrix.MultiplyPoint(combineEyeGazeOrigin));
            if (DebugLog) Debug.Log(combineEyeGazeOriginInWorldSpace.ToString());
            combineEyeGazeVectorInWorldSpace = originPoseMatrix.MultiplyVector(headPoseMatrix.MultiplyVector(combineEyeGazeVector));
            if (DebugLog) Debug.Log(combineEyeGazeVectorInWorldSpace.ToString());

            GazeTargetControl(combineEyeGazeOriginInWorldSpace, combineEyeGazeVectorInWorldSpace, layerMask);

            yield return new WaitForSeconds(steptime);
        }
    }
    void GazeTargetControl(Vector3 origin, Vector3 vector, int layerMask)
    {
        if (Physics.SphereCast(origin, 0.15f, vector, out hitinfo, maxDistance, layerMask))
        {
            OnEyeTrackingEvent?.Invoke(combineEyeGazeOriginInWorldSpace, combineEyeGazeVectorInWorldSpace, hitinfo);

            if (DebugLog)
            {
                Debug.Log($"TW: gaze dot position {gazePoint.transform.position.ToString()}\n name {hitinfo.transform.name.ToString()}\n TW: hit distance {hitinfo.distance.ToString()} \n TW: xr origin {xrOrigin.position}");
            }

            if (showGazePoint && gazePoint != null)
            {
                gazePoint.gameObject.SetActive(true);
                gazePoint.transform.position = hitinfo.point;
            }
            else
            {
                gazePoint.gameObject.SetActive(false);
            }

            // if (selectedObj != null && selectedObj != hitinfo.transform)
            // {
            //     if(selectedObj.GetComponent<ETObject>()!=null)
            //         selectedObj.GetComponent<ETObject>().UnFocused();
            //     selectedObj = null;
            // }
            // else if (selectedObj == null)
            // {
            //     selectedObj = hitinfo.transform;
            //     if (selectedObj.GetComponent<ETObject>() != null)
            //         selectedObj.GetComponent<ETObject>().IsFocused();
            // }

        }
        else
        {
            if (DebugLog)
                Debug.Log("TW: Ray casting failed\t" + origin.ToString() + "\t" + vector.ToString());
            gazePoint.gameObject.SetActive(false);
            // if (selectedObj != null)
            // {
            //    if (selectedObj.GetComponent<ETObject>() != null)
            //         selectedObj.GetComponent<ETObject>().UnFocused();
            //     selectedObj = null;
            // }
        }
    }
}