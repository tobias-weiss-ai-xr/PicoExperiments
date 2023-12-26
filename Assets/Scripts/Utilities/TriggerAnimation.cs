using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAnimation : MonoBehaviour
{

    private Animator animator;
    [SerializeField]
    private string trigger = "StartPrint";

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Run()
    {
        animator.SetTrigger(trigger);
        Debug.Log("Animation " +
             trigger + " activated.");
    }
}
