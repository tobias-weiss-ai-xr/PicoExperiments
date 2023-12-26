using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventSource : MonoBehaviour
{

    public event EventHandler OnSpacePressed;


    public class OnSpacePressedEventArgs : EventArgs
    {
        public int spaceCount;
    }


    public event EventHandler<OnSpacePressedEventArgs> OnSpacePressedWithArgs;
    private int spaceCount;

    public delegate void FloatEvent(float f);
    public event FloatEvent OnFloatEvent;

    public event Action<bool, int> OnActionEvent;

    public UnityEvent OnUnityEvent;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpacePressed?.Invoke(this, EventArgs.Empty);

            spaceCount++;
            OnSpacePressedWithArgs?.Invoke(this, new OnSpacePressedEventArgs { spaceCount = spaceCount });

            OnFloatEvent?.Invoke(5.5f);

            OnActionEvent?.Invoke(true, 123);

            OnUnityEvent?.Invoke();
        }

    }
}
