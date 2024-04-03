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
    [SerializeField] private float motionSpeed = 1f;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private EyeTrackingManager et;
    private GameObject doors;
    Dictionary<string, float> productEtStatus = new Dictionary<string, float>
    {
        {"explorer", 0f},
        {"solid", 0f},
        {"plus", 0f},
        {"pro", 0f},
    };

    public bool DebugLog = true;
    private bool startedMoving = false;

    public float DurationTheshold = 10f;

    void Awake()
    {
        Main main = null;
        GameObject.Find("Main").TryGetComponent<Main>(out main);
        if (main != null)
        {
            AdaptiveAgent = main.AdaptiveAgent;
            if (DebugLog) print("AdaptiveAgent: " + AdaptiveAgent);
            doors = GameObject.Find("doors 1 agent");
        }
    }
    void Start()
    {
        et = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        movePositionTransform = GameObject.Find("MoveTarget").transform;
        animator = GetComponent<Animator>();
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
        if (hit.transform == null || hit.transform.name == null)
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
            productEtStatus[gazeTargetTag] += (1 / 24f);
            if (DebugLog) Debug.Log(gazeTargetTag + " " + productEtStatus[gazeTargetTag].ToString());
        }

        // Check if all products were hit
        bool allProductsHit = productEtStatus.Values.All(duration => duration > DurationTheshold);
        if (DebugLog) Debug.Log("All products hit: " + allProductsHit.ToString());
        if (allProductsHit)
        {
            MoveToConsumer();
            et.OnEyeTrackingEvent -= CheckEyeTrackingForProductHit;
        }
    }

    public void MoveToConsumer()
    {
        if (!startedMoving)
        {
            startedMoving = true;
            if (DebugLog) Debug.Log("Moving to customer");
            movePositionTransform = GameObject.Find("XR Origin").transform;
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            doors.SetActive(false); // Todo: Use a nice animation to open the door
        }
        et.OnEyeTrackingEvent -= CheckEyeTrackingForProductHit;
    }
    void Update()
    {
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
