using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class FeatureMapRaycaster : MonoBehaviour
{
    public event Action<Color> OnFeatureMapColor;
    Transform cameraTransform;

    [Header("AoI logging")]
    public bool aoiLogging = true;
    public bool fixationLogging = true;
    [SerializeField] string participantId = "P01";
    [Tooltip("Seconds a new area must stay stable before the switch commits")]
    [SerializeField] float minDwell = 0.1f;
    [Tooltip("Half-angle of the foveal sampling cone in degrees (Gaussian-weighted switch voting)")]
    [SerializeField] float foveaRadius = 1.0f;
    [Tooltip("Raw trace sample rate in Hz")]
    [SerializeField] float rawSampleRate = 10f;
    [Header("Fixation detection")]
    [Tooltip("Angular speed (deg/s) above which the ray counts as a saccade (validated range for VR: 20-35")]
    [SerializeField] float saccadeVelocity = 30f;
    [Tooltip("Minimum fixation length in seconds to be logged")]
    [SerializeField] float minFixationDuration = 0.1f;
    [Header("Gaze source")]
    [Tooltip("Cast the AoI ray along the (combined, world-space) PICO eye-gaze direction instead of head forward when valid eye data is available; falls back to head forward otherwise")]
    [SerializeField] bool useEyeTracking = false;
    [Header("Attention heatmap")]
    [SerializeField] bool attentionHeatmap = false;
    [SerializeField] float heatmapGain = 1.0f;

    // writers (AoI transitions, raw trace, fixations)
    StreamWriter _aoiWriter, _rawWriter, _fixWriter;

    // attention heatmap: per-renderer accumulation textures keyed by renderer instance ID
    // attention heatmap: raw hit counts per renderer (CPU-side), painted to an
    // RGBA emission texture on the raw-sample cadence with a log ramp
    // Attention heatmaps live at a fixed low resolution: bounded paint cost
    // and memory regardless of the feature-map size (2048^2 buffers = ~100 MB
    // alloc + multi-million-texel repaints froze the frame on first hit).
    const int HeatmapSize = 256;
    Dictionary<int, Texture2D> _heatmapTex = new Dictionary<int, Texture2D>();
    Dictionary<int, float[]> _heatCounts = new Dictionary<int, float[]>();
    Dictionary<int, Color[]> _heatPixels = new Dictionary<int, Color[]>();
    Dictionary<int, float> _heatMax = new Dictionary<int, float>();
    HashSet<int> _heatDirty = new HashSet<int>();
    static readonly RaycastHit[] _rayHits = new RaycastHit[16]; // reused, no per-frame GC

    // current committed area + its start
    string _currentAoi = "None";
    string _currentHitObject = "";
    long _currentStartMs;

    // debounce: candidate area and when it first appeared
    string _pendingAoi;
    string _pendingHitObject;
    long _pendingSinceMs;

    // raw trace sampling
    long _nextRawSampleMs;
    long _nextHeatPaintMs;

    // fixation state machine
    Vector3 _prevDir;
    long _prevDirMs;
    bool _inFixation;
    long _fixStartMs;
    string _fixAoi;

    // eye-gaze source: latest valid world-space gaze dir from EyeTrackingManager
    EyeTrackingManager _eyeTracking;
    Vector3 _gazeDirWorld;
    long _gazeUpdateMs;
    const long EyeGazeFreshnessMs = 150; // manager streams at 24 Hz (~42 ms); tolerate a missed frame
    Vector3 _gazeFwd; // the ray actually used this frame (eye when fresh, else head) - cone-vote center

    // I-VT velocity smoothing: sliding ~20 ms window, decision on mean deg/s (IEEE VRW 2025)
    readonly List<float> _velDt = new List<float>();
    readonly List<float> _velDeg = new List<float>();
    float _velSumDt;
    const float VelWindowSec = 0.02f;

    static string LogTime() => DateTime.Now.ToString("HH:mm:ss.fff");
    static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    static string Sec(float seconds) => seconds.ToString("F3", CultureInfo.InvariantCulture);

    void Start()
    {
        cameraTransform = GameObject.Find("PlayerCameraRoot")?.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("FeatureMapRaycaster: no 'PlayerCameraRoot' in scene; AoI tracking disabled.");
            enabled = false;
            return;
        }

        if (aoiLogging || fixationLogging)
            StartAoiLog();

        if (useEyeTracking)
        {
            // Convention in this project (EyeTrackingInfo/Logging): the manager lives on a
            // GameObject named "EyeTracking". Only its read-only event is consumed here.
            _eyeTracking = GameObject.Find("EyeTracking")?.GetComponent<EyeTrackingManager>();
            if (_eyeTracking != null)
                _eyeTracking.OnEyeTrackingEvent += OnEyeGazeEvent;
            else
                Debug.LogWarning("FeatureMapRaycaster: useEyeTracking is on but no GameObject named 'EyeTracking' with an EyeTrackingManager was found; falling back to head gaze.");
        }

        _prevDir = cameraTransform.forward;
        _prevDirMs = NowMs();
        _gazeFwd = cameraTransform.forward;
    }

    void StartAoiLog()
    {
#if UNITY_EDITOR
        string logPath = Application.dataPath + "/../Recordings/";
#else
        // persistentDataPath is writable on device and matches EyeTrackingLogging's
        // convention (dataPath on Android is the APK dir - not writable)
        string logPath = Application.persistentDataPath + "/Recordings/";
#endif
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string baseName = $"{now:yyyy-MM-dd-HH-mm-ss}-{participantId}-{gameObject.scene.name}-aoi";

        // Self-describing recording: pipeline knobs + feature map info for replicability
        int texW, texH;
        AoiSessionManifest manifest = new AoiSessionManifest
        {
            pipeline = "aoi-v4",
            participantId = participantId,
            scene = gameObject.scene.name,
            timestampUtcEpochMs = NowMs(),
            timestampLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            unityVersion = Application.version,
            minDwell = minDwell,
            foveaRadius = foveaRadius,
            rawSampleRate = rawSampleRate,
            saccadeVelocity = saccadeVelocity,
            minFixationDuration = minFixationDuration,
            useEyeTracking = useEyeTracking,
            attentionHeatmap = attentionHeatmap,
            heatmapGain = heatmapGain,
            featureMapTexture = FindFeatureMapTexture(out texW, out texH),
            featureMapWidth = texW,
            featureMapHeight = texH,
        };
        File.WriteAllText(UniquePath(logPath, baseName + "-session.json"), JsonUtility.ToJson(manifest, true));

        if (aoiLogging)
        {
            _aoiWriter = new StreamWriter(UniquePath(logPath, baseName + ".csv"));
            _aoiWriter.WriteLine("StartEpochMs;LogTime;DurationInSec;Area;HitObject");
            _currentStartMs = NowMs();

            _rawWriter = new StreamWriter(UniquePath(logPath, baseName + "-raw.csv"));
            _rawWriter.WriteLine("EpochMs;LogTime;Area;HitObject;PointX;PointY;PointZ;U;V");
            _nextRawSampleMs = NowMs();
        }
        if (fixationLogging)
        {
            _fixWriter = new StreamWriter(UniquePath(logPath, baseName + "-fixations.csv"));
            _fixWriter.WriteLine("StartEpochMs;EndEpochMs;DurationInSec;Area");
        }
        Debug.Log($"AoI log started at: {logPath}{baseName}*.csv");
    }

    static string UniquePath(string logPath, string fileName)
    {
        string path = logPath + fileName;
        if (!File.Exists(path))
            return path;
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName);
        for (int i = 1; ; i++)
        {
            path = $"{logPath}{stem}-{i}{ext}";
            if (!File.Exists(path))
                return path;
        }
    }

    // Texel colors may be interpolated, so classify by the dominant channel
    static string ClassifyAoi(Color c)
    {
        if (c.r >= c.g && c.r >= c.b) return c.r > 0.5f ? "Details" : "None";
        if (c.g >= c.r && c.g >= c.b) return "Advertisement";
        return "Logo";
    }

    void Update()
    {
        long nowMs = NowMs();

        Vector3 fwd = cameraTransform.TransformDirection(Vector3.forward);
        bool eyeRay = _eyeTracking != null && nowMs - _gazeUpdateMs <= EyeGazeFreshnessMs;
        if (eyeRay)
            fwd = _gazeDirWorld;

        string area = ClassifyRay(cameraTransform.position, fwd, out RayHit hit, out Color color);
        _gazeFwd = fwd;
        if (color != Color.clear)
            OnFeatureMapColor?.Invoke(color);
        Debug.DrawRay(cameraTransform.position, fwd * 20f, eyeRay ? Color.blue : (area == "None" ? Color.green : Color.red));
        ObserveArea(area, nowMs, hit.hitObject);

        if (_rawWriter != null && nowMs >= _nextRawSampleMs)
        {
            Vector3 p = hit.point;
            _rawWriter.WriteLine($"{nowMs};{LogTime()};{_currentAoi};{hit.hitObject};" +
                $"{p.x.ToString("F2", CultureInfo.InvariantCulture)};{p.y.ToString("F2", CultureInfo.InvariantCulture)};{p.z.ToString("F2", CultureInfo.InvariantCulture)};" +
                $"{hit.uv.x.ToString("F4", CultureInfo.InvariantCulture)};{hit.uv.y.ToString("F4", CultureInfo.InvariantCulture)}");
            _rawWriter.Flush();
            _nextRawSampleMs = nowMs + (long)(1000f / Mathf.Max(rawSampleRate, 0.01f));
        }

        if (attentionHeatmap && _heatDirty.Count > 0 && nowMs >= _nextHeatPaintMs)
        {
            PaintHeatmaps();
            _nextHeatPaintMs = nowMs + 100; // 10 Hz repaint cap, independent of the raw writer
        }

        if (_fixWriter != null)
            UpdateFixation(nowMs, fwd);
    }

    // One raycast + feature map texel classification. color is Color.clear when no
    // feature map surface was hit. Geometry (hit point, uv) is carried in RayHit
    // so the raw trace can log it for post-hoc surface mapping.
    struct RayHit
    {
        public string hitObject;
        public Vector3 point;
        public Vector2 uv;
    }

    string ClassifyRay(Vector3 origin, Vector3 direction, out RayHit hit, out Color color, bool countHeat = true)
    {
        color = Color.clear;
        hit = default;
        
        // All hits along the ray, nearest first. Static buffer: no per-frame GC.
        int n = Physics.RaycastNonAlloc(origin, direction, _rayHits, 20f);
        if (n == 0)
            return "None";
        for (int i = 1; i < n; i++) // insertion sort, n is tiny
        {
            RaycastHit h = _rayHits[i];
            int j = i - 1;
            while (j >= 0 && _rayHits[j].distance > h.distance)
            {
                _rayHits[j + 1] = _rayHits[j];
                j--;
            }
            _rayHits[j + 1] = h;
        }
        
        // Separate opaque and transparent hits
        Renderer firstOpaqueRend = null;
        RaycastHit firstOpaqueHit = default;
        Renderer firstTransparentRend = null;
        RaycastHit firstTransparentHit = default;
        
        for (int i = 0; i < n; i++)
        {
            RaycastHit rayHit = _rayHits[i];
            Renderer rend = rayHit.transform.GetComponent<Renderer>();
            if (rend == null || rend.sharedMaterial == null)
                continue;
            
            // Check if material is transparent (render queue >= 3000 or transparent rendering mode)
            bool isTransparent = rend.sharedMaterial.renderQueue >= 3000 ||
                                  rend.sharedMaterial.GetTag("RenderType", false) == "Transparent";
            
            if (!isTransparent && firstOpaqueRend == null)
            {
                firstOpaqueRend = rend;
                firstOpaqueHit = rayHit;
            }
            else if (isTransparent && firstTransparentRend == null)
            {
                firstTransparentRend = rend;
                firstTransparentHit = rayHit;
            }
            
            // Early out: we have both, no need to check further
            if (firstOpaqueRend != null && firstTransparentRend != null)
                break;
        }
        
        // Prefer opaque hit, fall back to transparent
        Renderer targetRend = firstOpaqueRend ?? firstTransparentRend;
        RaycastHit targetHit = firstOpaqueRend != null ? firstOpaqueHit : firstTransparentHit;
        
        if (targetRend == null || targetRend.sharedMaterial == null ||
            targetRend.sharedMaterial.shader.name != "Universal Render Pipeline/FeatureMap")
            return "None";
        
        Collider collider = targetHit.collider;
        if (collider == null)
            return "None";
        
        hit.hitObject = targetHit.transform.name;
        hit.point = targetHit.point;
        hit.uv = targetHit.textureCoord;

        Texture2D tex = targetRend.material.GetTexture("_FeatureMap") as Texture2D;
        if (tex == null)
            return "None"; // FeatureMap shader without an assigned texture
        Vector2 texel = targetHit.textureCoord;
        texel.x *= tex.width;
        texel.y *= tex.height;
        color = tex.GetPixel((int)texel.x, (int)texel.y);
        
        if (attentionHeatmap && countHeat && color != Color.clear)
        {
            int rid = targetRend.GetInstanceID();
            if (!_heatmapTex.TryGetValue(rid, out Texture2D heatTex))
            {
                heatTex = new Texture2D(HeatmapSize, HeatmapSize, TextureFormat.RGBA32, false);
                _heatmapTex[rid] = heatTex;
                _heatCounts[rid] = new float[HeatmapSize * HeatmapSize];
                _heatPixels[rid] = new Color[HeatmapSize * HeatmapSize]; // Color.black default
                _heatMax[rid] = 0f;
                heatTex.SetPixels(_heatPixels[rid]);
                heatTex.Apply(false);
                targetRend.material.SetTexture("_EmissionMap", heatTex);
                targetRend.material.SetColor("_EmissionColor", Color.white); // default is black = invisible
                targetRend.material.EnableKeyword("_EMISSION");
            }
            Vector2 huv = targetHit.textureCoord;
            int px = Mathf.Clamp((int)(huv.x * HeatmapSize), 0, HeatmapSize - 1);
            int py = Mathf.Clamp((int)(huv.y * HeatmapSize), 0, HeatmapSize - 1);
            float[] counts = _heatCounts[rid];
            int idx = py * HeatmapSize + px;
            counts[idx] += heatmapGain;
            if (counts[idx] > _heatMax[rid])
                _heatMax[rid] = counts[idx];
            _heatDirty.Add(rid);
        }
        
        return ClassifyAoi(color);
    }

    // Tone-map raw counts to a black->red->white ramp (log-normalized) and
    // upload only textures that received hits since the last paint.
    void PaintHeatmaps()
    {
        foreach (int rid in _heatDirty)
        {
            Texture2D heatTex = _heatmapTex[rid];
            float[] counts = _heatCounts[rid];
            Color[] px = _heatPixels[rid];
            float norm = Mathf.Log(1f + _heatMax[rid]);
            if (norm <= 0f)
                continue;
            for (int i = 0; i < px.Length; i++)
            {
                float t = Mathf.Log(1f + counts[i]) / norm;
                px[i] = t < 0.5f
                    ? Color.Lerp(Color.black, Color.red, t * 2f)
                    : Color.Lerp(Color.red, Color.white, (t - 0.5f) * 2f);
            }
            heatTex.SetPixels(px);
            heatTex.Apply(false);
        }
        _heatDirty.Clear();
    }

    // Debounced area tracking: a new area only commits after holding minDwell
    void ObserveArea(string area, long nowMs, string hitObject)
    {
        if (area != _pendingAoi)
        {
            _pendingAoi = area;
            _pendingHitObject = hitObject;
            _pendingSinceMs = nowMs;
            return;
        }
        if (area != _currentAoi && nowMs - _pendingSinceMs >= (long)(minDwell * 1000f))
        {
            VerifySwitch(area, nowMs);
        }
    }

    // Foveal cone voting (view cone sampling, VCS): committing a switch requires
    // a Gaussian-weighted ray bundle (center + 8 rays at 0.5r + 8 at r, r =
    // foveaRadius) to agree on the new area with >= 75% of total weight. A ray
    // straddling an area border splits the cone and stays below the ratio (the
    // center ray's full weight makes 0.75, not 0.6, the smallest straddle-proof
    // bar), so the Details<->Advertisement flip-flop seen on real sessions is
    // suppressed. A failed vote restarts the dwell hold; a genuine area entry
    // with one foveal radius of clearance commits on the first attempt.
    void VerifySwitch(string area, long nowMs)
    {
        Vector3 origin = cameraTransform.position;
        Vector3 fwd = _gazeFwd; // center the cone on the ray that proposed the area (eye ray when active)
        Vector3 right = cameraTransform.TransformDirection(Vector3.right);
        Vector3 up = cameraTransform.TransformDirection(Vector3.up);

        float sigma = foveaRadius * 0.5f;
        float totalWeight = 0f, agreeWeight = 0f;
        for (int i = 0; i < 17; i++)
        {
            float angle = i == 0 ? 0f : (i <= 8 ? foveaRadius * 0.5f : foveaRadius);
            float phi = (i == 0 ? 0f : (i - 1) % 8) * Mathf.PI / 4f;
            Vector3 dir = (fwd + (right * Mathf.Cos(phi) + up * Mathf.Sin(phi)) *
                           Mathf.Tan(angle * Mathf.Deg2Rad)).normalized;
            float w = Mathf.Exp(-(angle * angle) / (2f * sigma * sigma));
            totalWeight += w;
            if (ClassifyRay(origin, dir, out _, out _, countHeat: false) == area)
                agreeWeight += w;
        }

        if (agreeWeight / totalWeight >= 0.75f)
            SwitchAoi(area, nowMs, _pendingHitObject);
        else
            _pendingSinceMs = nowMs; // border straddle: keep current area, retry after another dwell
    }

    void SwitchAoi(string newAoi, long nowMs, string hitObject)
    {
        if (_aoiWriter != null)
        {
            float duration = (nowMs - _currentStartMs) / 1000f;
            _aoiWriter.WriteLine($"{_currentStartMs};{LogTime()};{Sec(duration)};{_currentAoi};{_currentHitObject}");
            _aoiWriter.Flush(); // rows are rare; flush each so a crash loses nothing
        }
        _currentAoi = newAoi;
        _currentHitObject = hitObject;
        _currentStartMs = nowMs;
    }

    void UpdateFixation(long nowMs, Vector3 dir)
    {
        float dt = (nowMs - _prevDirMs) / 1000f;
        if (dt <= 0f)
            return;
        float deg = Vector3.Angle(_prevDir, dir);
        _prevDir = dir;
        _prevDirMs = nowMs;

        // I-VT smoothing: saccade test on mean deg/s over a sliding ~20 ms
        // window (Tobii-style, IEEE VRW 2025) so one noisy frame can't trip
        // the threshold. Kept window stays <= VelWindowSec plus the newest sample.
        _velDt.Add(dt);
        _velDeg.Add(deg);
        _velSumDt += dt;
        while (_velDt.Count > 1 && _velSumDt - _velDt[0] >= VelWindowSec)
        {
            _velSumDt -= _velDt[0];
            _velDt.RemoveAt(0);
            _velDeg.RemoveAt(0);
        }
        float sumDeg = 0f;
        for (int i = 0; i < _velDeg.Count; i++)
            sumDeg += _velDeg[i];
        float velocity = sumDeg / _velSumDt;

        if (velocity > saccadeVelocity)
        {
            EndFixation(nowMs);
            return;
        }
        if (!_inFixation)
        {
            _inFixation = true;
            _fixStartMs = nowMs;
            _fixAoi = _currentAoi;
        }
        else if (_currentAoi != _fixAoi)
        {
            // area changed while gaze stayed stable: close this fixation, next sample opens the new one
            EndFixation(nowMs);
        }
    }

    void EndFixation(long nowMs)
    {
        if (!_inFixation)
            return;
        _inFixation = false;
        float duration = (nowMs - _fixStartMs) / 1000f;
        if (_fixWriter != null && duration >= minFixationDuration && _fixAoi != "None")
        {
            _fixWriter.WriteLine($"{_fixStartMs};{nowMs};{Sec(duration)};{_fixAoi}");
            _fixWriter.Flush();
        }
    }

    // Latest valid combined eye-gaze (world space) from EyeTrackingManager. The
    // manager only raises this when the PICO device reports valid data, so a
    // received-direction alone is treated as valid gaze.
    void OnEyeGazeEvent(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        _gazeDirWorld = direction;
        _gazeUpdateMs = NowMs();
    }

    private void OnDestroy()
    {
        if (_eyeTracking != null)
            _eyeTracking.OnEyeTrackingEvent -= OnEyeGazeEvent;
        long nowMs = NowMs();
        if (_aoiWriter != null)
            SwitchAoi("None", nowMs, ""); // close the open interval
        EndFixation(nowMs); // close the open fixation

        foreach (var writer in new[] { _aoiWriter, _rawWriter, _fixWriter })
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
            }
        }
        _aoiWriter = _rawWriter = _fixWriter = null;
        foreach (var kvp in _heatmapTex)
            Destroy(kvp.Value);
        _heatmapTex.Clear();
        _heatCounts.Clear();
        _heatPixels.Clear();
        _heatMax.Clear();
        _heatDirty.Clear();
    }

    // Self-describing recording metadata (aoi-v3) for replicability. JsonUtility-
    // compatible: [Serializable] + public fields, no Newtonsoft.
    [Serializable]
    class AoiSessionManifest
    {
        public string pipeline;
        public string participantId;
        public string scene;
        public long timestampUtcEpochMs;
        public string timestampLocal;
        public string unityVersion;
        public float minDwell;
        public float foveaRadius;
        public float rawSampleRate;
        public float saccadeVelocity;
        public float minFixationDuration;
        public bool useEyeTracking;
        public bool attentionHeatmap;
        public float heatmapGain;
        public string featureMapTexture;
        public int featureMapWidth;
        public int featureMapHeight;
    }

    // One-shot scene scan for the session manifest: first renderer using the
    // FeatureMap shader reports its texture size (0/empty when none is present).
    static string FindFeatureMapTexture(out int width, out int height)
    {
        foreach (Renderer rend in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            Material mat = rend.sharedMaterial;
            if (mat == null || mat.shader == null || mat.shader.name != "Universal Render Pipeline/FeatureMap")
                continue;
            Texture2D tex = mat.GetTexture("_FeatureMap") as Texture2D;
            if (tex != null)
            {
                width = tex.width;
                height = tex.height;
                return tex.name;
            }
        }
        width = height = 0;
        return null;
    }
}
