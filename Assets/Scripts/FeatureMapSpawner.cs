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
    private Transform _anchor;
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
        _anchor = anchor;

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

            // Pre-warm the emission shader variant at spawn. Gaze-hit time is
            // too late: the first _EMISSION compile stalls a frame, and on the
            // PICO that hitch makes the compositor recenter ("view rotates").
            // The raycaster swaps in its own heat texture on first hit.
            Texture2D prewarmHeat = new Texture2D(FeatureMapRaycaster.HeatmapSize,
                FeatureMapRaycaster.HeatmapSize, TextureFormat.RGBA32, false);
            var black = new Color[prewarmHeat.width * prewarmHeat.height];
            prewarmHeat.SetPixels(black);
            prewarmHeat.Apply(false);
            rend.material.SetTexture("_EmissionMap", prewarmHeat);
            rend.material.SetColor("_EmissionColor", Color.white);
            rend.material.EnableKeyword("_EMISSION");

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
        MeshFilter mf = _firstProductRend ? _firstProductRend.GetComponent<MeshFilter>() : null;
        if (platRend == null || mf == null || mf.sharedMesh == null)
            return;
        // Mesh bounds x lossyScale: exact at spawn time (renderer bounds can
        // lag a frame, which left the last product hanging off the edge).
        float productWidth = mf.sharedMesh.bounds.size.x * _firstProductRend.transform.lossyScale.x;
        float rowWidth = 1.1f * ((BoxPositions.Count - 1) * spacing + productWidth);
        float currentWidth = platRend.bounds.size.x;
        if (currentWidth > 0.001f)
        {
            Vector3 ls = plat.localScale;
            ls.x *= rowWidth / currentWidth;
            plat.localScale = ls;
        }
        // Center the platform under the row (row axis = anchor right; anchor
        // and platform are world-axis aligned in this scene)
        Vector3 pos = plat.position;
        pos.x = _anchor.position.x;
        plat.position = pos;
    }
}
