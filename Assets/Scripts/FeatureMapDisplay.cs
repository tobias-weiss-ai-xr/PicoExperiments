using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FeatureMapDisplay : MonoBehaviour
{
    TMP_Text tmpText;
    void Start()
    {
        tmpText = GameObject.Find("FeatureDisplay").GetComponent<TMP_Text>();
        FeatureMapRaycaster raycaster = GetComponent<FeatureMapRaycaster>();
        raycaster.OnFeatureMapColor += ShowFeatureMapText;
    }

    private void ShowFeatureMapText(Color colorA)
    {
        Color[] testColors = { Color.black, Color.red, Color.green, Color.blue };
        Color? color = null;
        foreach (Color colorB in testColors)
            if (checkColorApproxEqual(colorA, colorB)) color = colorA;
        var msg = "";
        if (color == Color.red) msg = "Details";
        else if (color == Color.green) msg = "Advertisement";
        else if (color == Color.blue) msg = "Logo";
        print(msg);
        tmpText.text = msg;
    }

    private bool checkColorApproxEqual(Color colorA, Color colorB)
    {
        if (Mathf.Approximately(colorA.r, colorB.r)
            && Mathf.Approximately(colorA.g, colorB.g)
            && Mathf.Approximately(colorA.b, colorB.b))
            return true;
        return false;
    }
}


