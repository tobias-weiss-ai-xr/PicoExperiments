using System.Collections;
using System.Collections.Generic;
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
    private Transform target;
    private Transform targetWelcome;

    void Awake()
    {
        target = GameObject.Find("MoveTarget").transform;
        targetWelcome = GameObject.Find("MoveTargetWelcome").transform;
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animator.SetFloat("MotionSpeed", motionSpeed);
        StartCoroutine(MoveToConsumer());
    }
    private IEnumerator MoveToConsumer()
    {
        yield return new WaitForSeconds(10f);
        target.position = targetWelcome.position;
    }

    void Update()
    {
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
