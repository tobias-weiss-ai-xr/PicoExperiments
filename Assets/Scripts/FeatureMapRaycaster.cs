using System;
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

    // writers (AoI transitions, raw trace, fixations)
    StreamWriter _aoiWriter, _rawWriter, _fixWriter;

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

    // fixation state machine
    Vector3 _prevDir;
    long _prevDirMs;
    bool _inFixation;
    long _fixStartMs;
    string _fixAoi;

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

        _prevDir = cameraTransform.forward;
        _prevDirMs = NowMs();
    }

    void StartAoiLog()
    {
#if UNITY_EDITOR
        string logPath = Application.dataPath + "/../Recordings/";
#else
        string logPath = Application.dataPath + "/Recordings/";
#endif
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);

        DateTime now = DateTime.Now;
        string baseName = $"{now:yyyy-MM-dd-HH-mm-ss}-{participantId}-{gameObject.scene.name}-aoi";

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

        string area = ClassifyRay(cameraTransform.position, cameraTransform.TransformDirection(Vector3.forward), out RayHit hit, out Color color);
        if (color != Color.clear)
            OnFeatureMapColor?.Invoke(color);
        Debug.DrawRay(cameraTransform.position, cameraTransform.TransformDirection(Vector3.forward) * 20f, area == "None" ? Color.green : Color.red);
        ObserveArea(area, nowMs, hit.hitObject);

        if (_rawWriter != null && nowMs >= _nextRawSampleMs)
        {
            string p = hit.point;
            _rawWriter.WriteLine($"{nowMs};{LogTime()};{_currentAoi};{hit.hitObject};" +
                $"{p.x.ToString("F2", CultureInfo.InvariantCulture)};{p.y.ToString("F2", CultureInfo.InvariantCulture)};{p.z.ToString("F2", CultureInfo.InvariantCulture)};" +
                $"{hit.uv.x.ToString("F4", CultureInfo.InvariantCulture)};{hit.uv.y.ToString("F4", CultureInfo.InvariantCulture)}");
            _rawWriter.Flush();
            _nextRawSampleMs = nowMs + (long)(1000f / Mathf.Max(rawSampleRate, 0.01f));
        }

        if (_fixWriter != null)
            UpdateFixation(nowMs, cameraTransform.forward);
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

    string ClassifyRay(Vector3 origin, Vector3 direction, out RayHit hit, out Color color)
    {
        color = Color.clear;
        hit = default;
        if (!Physics.Raycast(origin, direction, out RaycastHit rayHit, 20f))
            return "None";
        hit.hitObject = rayHit.transform.name;
        hit.point = rayHit.point;
        hit.uv = rayHit.textureCoord;

        Renderer rend = rayHit.transform.GetComponent<Renderer>();
        Collider collider = rayHit.collider;
        if (rend == null || rend.sharedMaterial == null ||
            rend.sharedMaterial.shader.name != "Universal Render Pipeline/FeatureMap" || collider == null)
            return "None";

        Texture2D tex = rend.material.GetTexture("_FeatureMap") as Texture2D;
        Vector2 texel = rayHit.textureCoord;
        texel.x *= tex.width;
        texel.y *= tex.height;
        color = tex.GetPixel((int)texel.x, (int)texel.y);
        return ClassifyAoi(color);
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
        Vector3 fwd = cameraTransform.TransformDirection(Vector3.forward);
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
            if (ClassifyRay(origin, dir, out _, out _) == area)
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
        float velocity = Vector3.Angle(_prevDir, dir) / dt;
        _prevDir = dir;
        _prevDirMs = nowMs;

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

    private void OnDestroy()
    {
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
    }
}
