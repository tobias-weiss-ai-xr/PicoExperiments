using UnityEngine;

public class DrawGizmo : MonoBehaviour
{
    public Vector3 OutDir1 = new Vector3(.25f, 0, 1);
    public Vector3 OutDir2 = new Vector3(0, .25f, 1);
    private float dist = 5f;
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.forward * dist);
        Gizmos.DrawRay(transform.position, OutDir1 * dist);
        Gizmos.DrawRay(transform.position, OutDir2 * dist);
        // length roughly 1.5
        Gizmos.color = Color.green;
        Vector3 pt = transform.position + Vector3.forward;
        Vector3 pt1 = transform.position + OutDir1;
        Gizmos.DrawRay(transform.position, Vector3.forward);
        Gizmos.DrawRay(transform.position, OutDir1);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(pt1, (pt - pt1));
        print((pt - pt1));
        print((pt - pt1).magnitude);
    }
}
