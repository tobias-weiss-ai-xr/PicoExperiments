using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Normal.Realtime;
using System.Linq;

public class SpawnObject : MonoBehaviour
{
    // public string spawnObjectName;
    public GameObject spawnPoint;
    public GameObject spawnObject;
    public int delay = 3;
    private RealtimeTransform realtimeTransform;
    private RealtimeView spawnObjectView;

    public void SpawnRabbit()
    {
        Invoke("SpawnWithDelay", delay);
    }

    void SpawnWithDelay()
    {
        if (spawnPoint == null)
        {
            var name = "Spawn";
            var allKids = GetComponentsInChildren<Transform>();
            spawnPoint = allKids.Where(k => k.gameObject.name == name).FirstOrDefault().gameObject;

        }

        /* obsolete networking code
          
        Realtime.InstantiateOptions options = new Realtime.InstantiateOptions();
        options.ownedByClient = true;
        spawnObject = Realtime.Instantiate(spawnObjectName, options);

        spawnObjectView = spawnObject.GetComponent<RealtimeView>();

        // Get the RealtimeTransform component
        realtimeTransform = spawnObject.GetComponent<RealtimeTransform>();

        realtimeTransform.RequestOwnership();
        transform.SetParent(spawnPoint.transform);
        realtimeTransform.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
        //realtimeTransform.ClearOwnership();
        */

        Instantiate(spawnObject, spawnPoint.transform);

        Debug.Log($"Object {spawnObject.name} activated.");
    }
}
