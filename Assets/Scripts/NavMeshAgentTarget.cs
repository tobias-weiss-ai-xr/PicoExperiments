using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentTarget : MonoBehaviour
{
    public bool AdaptiveAgent = false;
    [SerializeField]
    private Transform movePositionTransform;
    [SerializeField] private float motionSpeed = 0.9f;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private EyeTrackingManager et;
    private GameObject doors;
    Dictionary<string, bool> productEtStatus = new Dictionary<string, bool>
    {
        {"explorer", false},
        {"solid", false},
        {"plus", false},
        {"pro", false},
    };

    public bool DebugLog = true;
    private bool startedMoving = false;

    void Start()
    {
        AdaptiveAgent = GameObject.Find("Main").GetComponent<Main>().AdaptiveAgent;
        if (DebugLog) print("AdaptiveAgent: " + AdaptiveAgent);
        et = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        doors = GameObject.Find("doors 1 agent");
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        animator.SetFloat("MotionSpeed", motionSpeed);
        if (!AdaptiveAgent)
        {
            MoveToConsumer();
        }
        else
        {
            et.OnEyeTrackingEvent += CheckEyeTrackingForProductHit;
        }
    }

    private void CheckEyeTrackingForProductHit(Vector3 origin, Vector3 direction, RaycastHit hit)
    {
        if (hit.transform.name == null)
        {
            if (DebugLog) Debug.Log("No ET hit found");
            return; // Do nothing if no transform
        }
        string gazeTarget = hit.transform.name;
        string gazeTargetTag = hit.transform.tag;
        if (DebugLog) Debug.Log("ET hit found: " + gazeTarget + " tag: " + gazeTargetTag);
        if (productEtStatus.ContainsKey(gazeTargetTag))
        {
            if (DebugLog) Debug.Log("Product hit: " + gazeTargetTag);
            productEtStatus[gazeTargetTag] = true;
        }

        // Check if all Products hit
        bool allProductsHit = productEtStatus.Values.All(product => product == true);
        if (DebugLog) Debug.Log("All products hit: " + allProductsHit.ToString());
        if (allProductsHit) MoveToConsumer();
    }

    public void MoveToConsumer()
    {
        if (!startedMoving)
        {
            startedMoving = true;
            if (DebugLog) Debug.Log("Moving to customer");
            movePositionTransform = GameObject.Find("XR Origin").transform;
            navMeshAgent.SetDestination(movePositionTransform.position);
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            doors.SetActive(false); // Todo: Use a nice animation...
        }
        et.OnEyeTrackingEvent -= CheckEyeTrackingForProductHit;
    }

    void Update()
    {
    }
}
