using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.XR;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using System.Collections;

public class EyeTrackingManager : MonoBehaviour
{
    public Transform Origin;
    public GameObject SpotLight;
    public Transform gazePoint;
    private Matrix4x4 headPoseMatrix;
    private Matrix4x4 originPoseMatrix;

    private RaycastHit hitinfo;

    private Transform selectedObj;

    private bool wasPressed;
    TrackingStateCode trackingState;
    private bool supported = false;

    public bool DebugLog = true;
    Matrix4x4 matrix;

    public bool UseGazeDot = false;

    // Logging
    public event EyeTrackingEvent OnEyeTrackingEvent;
    public delegate void EyeTrackingEvent(Vector3 origin, Vector3 direction, RaycastHit hit);

    void Start()
    {
        if (Origin == null) Origin = GameObject.Find("XR Origin").transform;
        if (gazePoint == null) gazePoint = GameObject.Find("gazePoint").transform;
        originPoseMatrix = Origin.localToWorldMatrix;
        trackingState = (TrackingStateCode)PXR_MotionTracking.WantEyeTrackingService();
        // Query if the current device supports eye tracking
        EyeTrackingMode eyeTrackingMode = EyeTrackingMode.PXR_ETM_NONE;
        int supportedModesCount = 0;
        trackingState = (TrackingStateCode)PXR_MotionTracking.GetEyeTrackingSupported(ref supported, ref supportedModesCount, ref eyeTrackingMode);
        StartCoroutine(EyeTracking(1 / 24f));  // 1/24=0.04 sec.=24FPS (everything else leads to instabilities!)
    }

    IEnumerator EyeTracking(float stepTime)
    {
        while (true)
        {
            if (Camera.main)
            {
                matrix = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
            }
            else
            {
                matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            }
            // Single combined query; only use the values when the device reports them as valid
            if (PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 gazeOrigin) &&
                PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 gazeDirection))
            {
                var originOffset = matrix.MultiplyPoint(gazeOrigin);
                var directionOffset = matrix.MultiplyVector(gazeDirection);

                Ray ray = new Ray(originOffset, directionOffset);
                if (Physics.Raycast(ray, out RaycastHit hit, 20))
                {
                    if (UseGazeDot) gazePoint.gameObject.SetActive(true);
                    gazePoint.position = hit.point;
                }
                else
                {
                    gazePoint.gameObject.SetActive(false);
                }
                // Event provider for logging, etc.
                OnEyeTrackingEvent?.Invoke(originOffset, directionOffset, hit);
            }
            yield return new WaitForSeconds(stepTime);
        }
    }
}
