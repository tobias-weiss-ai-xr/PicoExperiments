using System.Globalization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionOnTimer : MonoBehaviour
{
    private Action timerCallback;
    private float timer;
    public void SetTimer(float timer, Action timerCallback)
    {
        this.timer = timer;
        this.timerCallback = timerCallback;
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) timerCallback();
        }
    }
}
