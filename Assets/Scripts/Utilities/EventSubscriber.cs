using System.Diagnostics.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSubscriber : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        EventSource eventSource = GetComponent<EventSource>();
        eventSource.OnSpacePressed += eventSource_OnSpacePressed;
        eventSource.OnSpacePressedWithArgs += eventSource_OnSpacePressedWithArgs;
        eventSource.OnFloatEvent += eventSource_OnFloatEvent;
        eventSource.OnActionEvent += eventSource_OnActionEvent;
    }

    // Update is called once per frame
    private void eventSource_OnSpacePressed(object sender, EventArgs e)
    {
        Debug.Log("Space pressed");
    }
    private void eventSource_OnSpacePressedWithArgs(object sender, EventSource.OnSpacePressedEventArgs e)
    {
        Debug.Log($"Space pressed {e.spaceCount} times");
    }

    private void eventSource_OnFloatEvent(float f)
    {
        Debug.Log("Float event: " + f.ToString());
    }

    private void eventSource_OnActionEvent(bool arg1, int arg2)
    {
        Debug.Log($"Arg1: {arg1}, Arg2: {arg2}");
    }

    public void eventSource_OnUnityEvent()
    {
        Debug.Log("Unity event");
    }
}
