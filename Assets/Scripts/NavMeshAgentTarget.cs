using System.Collections;
using System.Collections.Generic;
using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentTarget : MonoBehaviour
{
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
        StartCoroutine(MoveToConsumer());
    }
    private IEnumerator MoveToConsumer()
    {
        yield return new WaitForSeconds(5f);
        movePositionTransform = GameObject.Find("XR Origin").transform;
        doors.SetActive(false); // Todo: Use a nice animation...
    }
    void Update()
    {
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
