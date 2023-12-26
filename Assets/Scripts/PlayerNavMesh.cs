using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerNavMesh : MonoBehaviour
{
    [SerializeField]
    private Transform movePositionTransform;
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
