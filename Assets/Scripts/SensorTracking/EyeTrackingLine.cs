using UnityEngine;

public class EyeTrackingLine : MonoBehaviour
{
    public LineRenderer lineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        EyeTrackingManager et = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        // et.OnEyeTrackingEvent += DrawLine;
    }

    private void DrawLine(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, hit.point);
    }
}
