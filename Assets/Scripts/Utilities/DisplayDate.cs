using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class DisplayDate : MonoBehaviour
{
    private TMP_Text dateText;

    float slowUpdateRate = 0.2f;

    void Start()
    {
        dateText = GetComponent<TMP_Text>();
        InvokeRepeating("SlowUpdate", 0.0f, slowUpdateRate);
    }

    void SlowUpdate()
    {
        dateText.text = DateTime.Now.ToString();
    }
}
