using UnityEngine;

public class FeatureMapSpawner : MonoBehaviour
{
    private GameObject instance;
    private Shader shader;
    void Awake()
    {
        shader = Shader.Find("Universal Render Pipeline/FeatureMap");

        // Prototype (Resources.Load works in editor and player builds)
        GameObject prototype = Resources.Load<GameObject>("FeatureMapDemo/DemoBox");
        if (prototype == null)
        {
            Debug.LogError("Prototype 'FeatureMapDemo/DemoBox' not found in Resources.");
            return;
        }

        // Instance
        instance = GameObject.Instantiate(prototype);
        instance.AddComponent<MeshCollider>();
        GameObject handle = new GameObject();
        handle.transform.SetParent(instance.transform);
        handle.transform.localPosition = new Vector3(-0.0022f, -0.0022f, -0.0022f);
        Vector3 targetPosition = GameObject.Find("Spawn").transform.position;
        instance.transform.position = targetPosition + (instance.transform.position - handle.transform.position);
        MeshRenderer rend = instance.GetComponent<MeshRenderer>();
        rend.material.shader = shader;

        // Feature map needs Read/Write enabled on import (set in its .meta)
        Texture2D tex = Resources.Load<Texture2D>("FeatureMapDemo/FeatureMap");
        if (tex == null)
        {
            Debug.LogError("Feature map 'FeatureMapDemo/FeatureMap' not found in Resources.");
            return;
        }
        rend.material.SetTexture("_FeatureMap", tex);
    }
}
