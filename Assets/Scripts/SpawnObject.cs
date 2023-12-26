using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Normal.Realtime;

public class SpawnObject : MonoBehaviour
{
    public string spawnObjectName;
    public GameObject spawnPoint;
    private GameObject spawnObject;
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
            spawnPoint = GameObject.Find("Spawn");

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

        Debug.Log($"Object {spawnObject.name} activated.");
    }
}
