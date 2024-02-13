using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentTarget : MonoBehaviour
{
    bool TimedAppearance = false;
    [SerializeField]
    private Transform movePositionTransform;
    [SerializeField]
    private float motionSpeed = 0.9f;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private GameObject doors;

    void Awake()
    {
        doors = GameObject.Find("doors 1 agent");
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animator.SetFloat("MotionSpeed", motionSpeed);
        if (TimedAppearance)
        {
            StartCoroutine(MoveToConsumer(5f));
        }
    }
    public IEnumerator MoveToConsumer(float delay)
    {
        yield return new WaitForSeconds(delay);
        movePositionTransform = GameObject.Find("XR Origin").transform;
        doors.SetActive(false); // Todo: Use a nice animation...
    }


    void Update()
    {
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
