using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.XR;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using DG.Tweening;
using System.Collections;

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
        if (Origin == null) Origin = GameObject.Find("XR Origin").transform;
        if (gazePoint == null) gazePoint = GameObject.Find("gazePoint").transform;
        combineEyeGazeOriginOffset = Vector3.zero;
        combineEyeGazeVector = Vector3.zero;
        combineEyeGazeOrigin = Vector3.zero;
        originPoseMatrix = Origin.localToWorldMatrix;
#if Unity_ANDROID
        trackingState = (TrackingStateCode)PXR_MotionTracking.WantEyeTrackingService();
        // Query if the current device supports eye tracking
        EyeTrackingMode eyeTrackingMode = EyeTrackingMode.PXR_ETM_NONE;
        int supportedModesCount = 0;
        trackingState = (TrackingStateCode)PXR_MotionTracking.GetEyeTrackingSupported(ref supported, ref supportedModesCount, ref eyeTrackingMode);
        StartCoroutine(EyeTracking(1 / 24f));  // 1/24=0.04 sec.=24FPS (everything else leads to instabilities!)
#endif
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
            bool result = PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 Origin) && PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 Direction);
            PXR_EyeTracking.GetCombineEyeGazePoint(out Origin);
            PXR_EyeTracking.GetCombineEyeGazeVector(out Direction);
            var OriginOffset = matrix.MultiplyPoint(Origin);
            var DirectionOffset = matrix.MultiplyVector(Direction);
            if (result)
            {
                Ray ray = new Ray(OriginOffset, DirectionOffset);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 20))
                {
                    gazePoint.gameObject.SetActive(true);
                    gazePoint.DOMove(hit.point, Time.deltaTime).SetEase(Ease.Linear);
                }
                else
                {
                    gazePoint.gameObject.SetActive(false);
                }
                // Event provider for logging, etc.
                OnEyeTrackingEvent?.Invoke(OriginOffset, DirectionOffset, hit);
            }
            yield return new WaitForSeconds(stepTime);
        }
    }
}
