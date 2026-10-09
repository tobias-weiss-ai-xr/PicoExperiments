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
    [SerializeField] Material[] slotMaterials; // optional: distinct visuals per slot (real visual search); applied to slot i

    private Shader shader;
    private Renderer _firstProductRend;
    private Renderer _platRend;
    private Transform _anchor;
    readonly List<Transform> _spawned = new List<Transform>();
    void Awake()
    {
        BoxPositions.Clear(); // statics survive scene reloads in the editor

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
        _anchor = GameObject.Find("Spawn")?.transform;
        if (_anchor == null)
        {
            Debug.LogError("FeatureMapSpawner: no 'Spawn' object in scene.");
            return;
        }
        Transform plat = platform != null ? platform : GameObject.Find("Cube")?.transform;
        _platRend = plat ? plat.GetComponent<Renderer>() : null;

        for (int i = 0; i < count; i++)
        {
            // Row along the anchor's right vector, centered on the anchor
            Vector3 target = _anchor.position + _anchor.right * ((i - (count - 1) * 0.5f) * spacing);

            GameObject instance = Instantiate(prototype);
            instance.name = $"DemoBox_{i + 1}";
            instance.AddComponent<MeshCollider>();
            instance.transform.localScale *= boxScale;

            MeshRenderer rend = instance.GetComponent<MeshRenderer>();
            rend.material.shader = shader;
            rend.material.SetTexture("_FeatureMap", tex);
            if (slotMaterials != null && i < slotMaterials.Length && slotMaterials[i] != null)
            {
                // keep the pipeline's shader, swap the surface look per slot
                rend.material.color = slotMaterials[i].color;
                if (slotMaterials[i].HasProperty("_MainTex") && slotMaterials[i].mainTexture != null)
                    rend.material.mainTexture = slotMaterials[i].mainTexture;
            }

            // Place by measured bounds: product bottom-center onto the spawn
            // target (y = platform surface if available). Independent of the
            // FBX root transform and of the boxScale — no calibration magic.
            Bounds wb = WorldBounds(rend);
            float surfaceY = _platRend ? _platRend.bounds.max.y : target.y;
            instance.transform.position += new Vector3(
                target.x - wb.center.x,
                surfaceY - wb.min.y,
                target.z - wb.center.z);

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
            _spawned.Add(instance.transform);
            BoxPositions.Add(instance.transform.position);
        }

        FitPlatform();
    }

    // Exact world AABB from localBounds (renderer.bounds can lag a frame for
    // freshly instantiated objects).
    static Bounds WorldBounds(Renderer r)
    {
        Bounds l = r.localBounds;
        Vector3 mn = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 mx = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 c = r.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(
                x == 0 ? l.min.x : l.max.x,
                y == 0 ? l.min.y : l.max.y,
                z == 0 ? l.min.z : l.max.z));
            mn = Vector3.Min(mn, c);
            mx = Vector3.Max(mx, c);
        }
        Bounds b = new Bounds(mn, Vector3.zero);
        b.Encapsulate(mx);
        return b;
    }

    // Widen the platform under the row so all products fit on it. Assumes the
    // row axis (anchor right) and the platform are world-axis aligned (true
    // for the ObjectTracking scene's unrotated anchor + Cube).
    void FitPlatform()
    {
        if (_platRend == null || _firstProductRend == null)
            return;
        MeshFilter mf = _firstProductRend.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return;
        // Mesh bounds x lossyScale: exact at spawn time.
        float productWidth = mf.sharedMesh.bounds.size.x * _firstProductRend.transform.lossyScale.x;
        float rowWidth = 1.1f * ((BoxPositions.Count - 1) * spacing + productWidth);
        float currentWidth = _platRend.bounds.size.x;
        if (currentWidth > 0.001f)
        {
            Vector3 ls = _platRend.transform.localScale;
            ls.x *= rowWidth / currentWidth;
            _platRend.transform.localScale = ls;
        }
        // Center the platform under the row (row axis = anchor right; anchor
        // and platform are world-axis aligned in this scene)
        Vector3 pos = _platRend.transform.position;
        pos.x = _anchor.position.x;
        _platRend.transform.position = pos;
    }

    // Re-randomize product positions along the row (Fisher-Yates over the
    // existing row slots). Call between trials to prevent position learning;
    // the seed makes arrangements reproducible across participants.
    public void ShuffleRow(int seed)
    {
        var rnd = new System.Random(seed);
        for (int i = _spawned.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (_spawned[i].position, _spawned[j].position) = (_spawned[j].position, _spawned[i].position);
        }
        for (int i = 0; i < _spawned.Count; i++)
            BoxPositions[i] = _spawned[i].position; // keep the debug list in sync
    }
}
