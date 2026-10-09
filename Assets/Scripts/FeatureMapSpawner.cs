using System.Collections.Generic;
using UnityEngine;

public class FeatureMapSpawner : MonoBehaviour
{
    // Diagnostics: spawner outcome + all product locations (read by aoiDebugLog)
    public static readonly List<Vector3> BoxPositions = new List<Vector3>();

    [SerializeField] int count = 5;        // number of demo products, centered on Spawn
    [SerializeField] float spacing = 1.5f; // meters between neighboring products
    [SerializeField] float boxScale = 2f;  // uniform scale-up of each product (readability at distance)
    [SerializeField] Transform platform;   // box the products sit on; widened along the row to fit

    private Shader shader;
    private Renderer _firstProductRend;
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
        // Feature map needs Read/Write enabled on import (set in its .meta)
        Texture2D tex = Resources.Load<Texture2D>("FeatureMapDemo/FeatureMap");
        if (tex == null)
        {
            Debug.LogError("Feature map 'FeatureMapDemo/FeatureMap' not found in Resources.");
            return;
        }
        Transform anchor = GameObject.Find("Spawn")?.transform;
        if (anchor == null)
        {
            Debug.LogError("FeatureMapSpawner: no 'Spawn' object in scene.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // Row along the anchor's right vector, centered on the anchor
            Vector3 target = anchor.position + anchor.right * ((i - (count - 1) * 0.5f) * spacing);

            GameObject instance = Instantiate(prototype);
            instance.name = $"DemoBox_{i + 1}";
            instance.AddComponent<MeshCollider>();
            instance.transform.localScale *= boxScale;
            GameObject handle = new GameObject();
            handle.transform.SetParent(instance.transform);
            handle.transform.localPosition = new Vector3(-0.0022f, -0.0022f, -0.0022f);
            instance.transform.position = target + (instance.transform.position - handle.transform.position);

            MeshRenderer rend = instance.GetComponent<MeshRenderer>();
            rend.material.shader = shader;
            rend.material.SetTexture("_FeatureMap", tex);
            if (i == 0) _firstProductRend = rend;
            BoxPositions.Add(instance.transform.position);
        }

        FitPlatform();
    }

    // Widen the platform under the row so all products fit on it. Assumes the
    // row axis (anchor right) and the platform are world-axis aligned (true
    // for the ObjectTracking scene's unrotated anchor + Cube).
    void FitPlatform()
    {
        Transform plat = platform != null ? platform : GameObject.Find("Cube")?.transform;
        Renderer platRend = plat ? plat.GetComponent<Renderer>() : null;
        if (platRend == null || _firstProductRend == null)
            return;
        float productWidth = _firstProductRend.bounds.size.x;
        float rowWidth = (BoxPositions.Count - 1) * spacing + productWidth;
        float currentWidth = platRend.bounds.size.x;
        if (currentWidth > 0.001f)
        {
            Vector3 ls = plat.localScale;
            ls.x *= rowWidth / currentWidth;
            plat.localScale = ls;
        }
    }
}
