using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class FeatureMapRaycaster : MonoBehaviour
{
    public event Action<Color> OnFeatureMapColor;
    Transform cameraTransform;
    // float sphereRadius = 0.01f;
    void Start()
    {
        cameraTransform = GameObject.Find("PlayerCameraRoot").transform;
    }
    TMP_Text tmpText;
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
                return;

            // Instantiate object
            Texture2D tex = rend.material.GetTexture("_FeatureMap") as Texture2D;
            hitUV.x *= tex.width;
            hitUV.y *= tex.height;
            Color color = tex.GetPixel((int)hitUV.x, (int)hitUV.y);
            OnFeatureMapColor?.Invoke(color);
            // print("sender" + color.ToString());
        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.green);
        }
    }
}