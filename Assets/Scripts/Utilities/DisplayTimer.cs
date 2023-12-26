using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class DisplayTimer : MonoBehaviour
{
    public CountdownTimer timer;
    private TMP_Text timeText;

    float slowUpdateRate = 0.2f;

    void Start()
    {
        timeText = GetComponent<TMP_Text>();
        InvokeRepeating("SlowUpdate", 0.0f, slowUpdateRate);
    }

    void SlowUpdate()
    {
        if (timer != null && timeText != null)
        {
            float timeToDisplay = timer.TimeRemaining + 1;
            float minutes = Mathf.FloorToInt(timeToDisplay / 60);
            float seconds = Mathf.FloorToInt(timeToDisplay % 60);
            timeText.text = string.Format("Timer: {0:00}:{1:00}", minutes, seconds);
        }
    }
}
