using System;
using System.Collections;
using UnityEngine;
using Unity.XR.PXR;

public class EyeTracking : MonoBehaviour
{
    // Positions and targets
    private Transform gazePoint;
    private Transform xrOrigin;
    public bool showGazePoint = true;

    // Internals
    private Matrix4x4 matrix;

    // Logging
    public event EyeTrackingEvent OnEyeTrackingEvent;
    public delegate void EyeTrackingEvent(Vector3 origin, Vector3 direction, RaycastHit hit);

    void Start()
    {
        gazePoint = GameObject.Find("gazePoint").transform;
        xrOrigin = GameObject.Find("XR Origin").transform;
        StartCoroutine(EyeRaycast(0.01f)); // default: 0.04 sec. = 24 FPS
    }

    IEnumerator EyeRaycast(float steptime)
    {
        while (true)
        {
#if !UNITY_EDITOR
            // PXR_EyeTracking.GetHeadPosMatrix(out matrix);
            if (Camera.main)
            {
                matrix = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
            }

            PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 origin);
            PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 direction);

            // Find Adjustment Index            // eyeTracking.OnEyeTrackingData += ReceiveEyeTrackingSample;n;
            var forwardPt = origin + Vector3.forward;
            var gazeDistance = (gazeVec - forwardPt).magnitude;
            if (gazeDistance > 0.25f)
            {
                if (gazeVec.x > 0 && gazeVec.y > 0) // top right
                    positionIndex = 1;
                else if (gazeVec.x < 0 && gazeVec.y > 0) // top left
                    positionIndex = 2;
                else if (gazeVec.x > 0 & gazeVec.y < 0) // bottom right
                    positionIndex = 3;
                else if (gazeVec.x < 0 & gazeVec.y < 0) // bottom left
                    positionIndex = 4;
            }

            var directionAdjusted = direction;
            if (Main.Instance.EyeTrackingDirectionAdjustments.Count > 0)
            {
                directionAdjusted += Main.Instance.EyeTrackingDirectionAdjustments[positionIndex];
            }

            var originOffset = matrix.MultiplyPoint(origin);
            var directionOffset = matrix.MultiplyVector(directionAdjusted);


            RaycastHit hit;
            Ray ray = new Ray(originOffset, directionOffset);

            // Everything except layer 2 (Raycast Ignore)
            int layer = 2;
            int layerMask = 1 << layer;
            layerMask = ~layerMask;

            if (Physics.Raycast(ray, out hit, 20, layerMask))
            {
                if (showGazePoint && gazePoint != null)
                {
                    gazePoint.gameObject.SetActive(true);
                    gazePoint.transform.position = hit.point;
                    Debug.Log($"TW: gaze dot position {gazePoint.transform.position.ToString()}\n name {hit.transform.name.ToString()}\n TW: hit distance {hit.distance.ToString()} \n TW: xr origin {xrOrigin.position}");
                }
                else
                {
                    gazePoint.gameObject.SetActive(false);
                }
            }
            else
            {
                gazePoint.gameObject.SetActive(false);
            }
            // Invoke logging event
            OnEyeTrackingEvent?.Invoke(originOffset, directionOffset, hit);
#endif
            yield return new WaitForSeconds(steptime);
        }
    }
}