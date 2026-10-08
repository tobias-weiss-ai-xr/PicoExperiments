using System.Collections.Generic;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using TMPro;

public class FeatureMapRaycaster : MonoBehaviour
{
    public event Action<Color> OnFeatureMapColor;
    Transform cameraTransform;
    // float sphereRadius = 0.01f;

    [Header("AoI logging")]
    public bool aoiLogging = true;
    StreamWriter _aoiWriter;
    string _currentAoi;
    DateTime _aoiStart;

    void Start()
    {
        cameraTransform = GameObject.Find("PlayerCameraRoot").transform;
        if (aoiLogging)
            StartAoiLog();
    }

    // Area-of-interest state log: one row per interval, area transitions only
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
        string fileName = $"{now:yyyy-MM-dd-HH}h{now:mm}m-{gameObject.scene.name}-aoi";
        _aoiWriter = new StreamWriter(logPath + fileName + ".csv");
        _aoiWriter.WriteLine("StartTime;EndTime;DurationInSec;Area");
        _aoiStart = now;
        _currentAoi = "None";
        Debug.Log("AoI log started at: " + logPath + fileName + ".csv");
    }

    // Texel colors may be interpolated, so classify by the dominant channel
    static string ClassifyAoi(Color c)
    {
        if (c.r >= c.g && c.r >= c.b) return c.r > 0.5f ? "Details" : "None";
        if (c.g >= c.r && c.g >= c.b) return "Advertisement";
        return "Logo";
    }

    void SwitchAoi(string newAoi)
    {
        if (!aoiLogging || _aoiWriter == null || newAoi == _currentAoi)
            return;
        DateTime now = DateTime.Now;
        _aoiWriter.WriteLine($"{_aoiStart:HH:mm:ss.fff};{now:HH:mm:ss.fff};{(now - _aoiStart).TotalSeconds.ToString(\"F3\", CultureInfo.InvariantCulture)};{_currentAoi}");
        _aoiWriter.Flush(); // rows are rare; flush each so a crash loses nothing
        _currentAoi = newAoi;
        _aoiStart = now;
    }

    void Update()
    {
        // if (Physics.SphereCast(cameraTransform.position, sphereRadius, cameraTransform.TransformDirection(Vector3.forward), out RaycastHit hit, 20f))
        if (Physics.Raycast(cameraTransform.position, cameraTransform.TransformDirection(Vector3.forward), out RaycastHit hit, 20f))
        {
            // Debug.Log($"Hit {hit.collider.transform.name}");
            // Debug.DrawRay(cameraTransform.position, cameraTransform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
            Debug.DrawRay(cameraTransform.position, cameraTransform.TransformDirection(Vector3.forward) * hit.distance, Color.red);

            Vector2 hitUV = hit.textureCoord;

            // Just in case, also make sure the collider also has a renderer
            // material and texture.
            Renderer rend = hit.transform.GetComponent<Renderer>();

            // Do not ignore primitive colliders
            Collider collider = hit.collider;

            // Null guard
            if (rend == null || rend.sharedMaterial == null ||
                rend.sharedMaterial.shader.name != "Universal Render Pipeline/FeatureMap" || collider == null)
            {
                SwitchAoi("None");
                return;
            }

            // Instantiate object
            Texture2D tex = rend.material.GetTexture("_FeatureMap") as Texture2D;
            hitUV.x *= tex.width;
            hitUV.y *= tex.height;
            Color color = tex.GetPixel((int)hitUV.x, (int)hitUV.y);
            OnFeatureMapColor?.Invoke(color);
            // print("sender" + color.ToString());
            SwitchAoi(ClassifyAoi(color));
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green);
            SwitchAoi("None");
        }
    }

    private void OnDestroy()
    {
        if (_aoiWriter != null)
        {
            SwitchAoi("None"); // close the open interval
            _aoiWriter.Flush();
            _aoiWriter.Close();
            _aoiWriter = null;
        }
    }
}