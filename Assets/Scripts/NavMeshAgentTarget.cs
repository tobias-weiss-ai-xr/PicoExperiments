using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Convai.Scripts;
using Convai.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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
    public float MinDuration = 30f;
    public float MaxDuration = 120f;
    float duration = 0f;

    void Awake()
    {
        if (GameObject.Find("Main") != null)
        {
            GameObject.Find("Main").TryGetComponent<Main>(out Main main);
            if (main)
            {
                AdaptiveAgent = main.AdaptiveAgent;
                if (DebugLog) print("AdaptiveAgent: " + AdaptiveAgent);
                doors = GameObject.Find("doors 1 agent");
            }
        }
    }
    void Start()
    {
        et = GameObject.Find("EyeTracking").GetComponent<EyeTrackingManager>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        movePositionTransform = GameObject.Find("MoveTarget").transform;
        animator = GetComponent<Animator>();
        if (SceneManager.GetActiveScene().name == "Preparation")
        {
            MoveToStaticTarget();
            
        }
        else { 
            if (!AdaptiveAgent)
            {
                MoveToConsumer();
            }
            else
            {
                et.OnEyeTrackingEvent += CheckEyeTrackingForProductHit;
            }
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

        // Check if rules are met
        bool allProductsHit = productEtStatus.Values.All(duration => duration > DurationTheshold);
        if (DebugLog) Debug.Log("All products hit: " + allProductsHit.ToString());
        if (duration > MaxDuration || (allProductsHit && duration > MinDuration))
        {
            MoveToConsumer();
            et.OnEyeTrackingEvent -= CheckEyeTrackingForProductHit;
        }
    }
    public void MoveToStaticTarget()
    {
        if (startedMoving)
            return;
        startedMoving = true;
        if (DebugLog) Debug.Log("Moving to static target");
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }

    public void MoveToConsumer()
    {
        if (!startedMoving)
        {
            startedMoving = true;
            if (DebugLog) Debug.Log("Moving to customer");
            movePositionTransform = GameObject.Find("XR Origin").transform;
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            if(doors!=null)
            {
                doors.SetActive(false); // Todo: Use a nice animation to open the door
            }
            GameObject.Find("DoorClientInfo").GetComponent<TMP_Text>().text = "Remember to press the trigger\n to ask questions!";
        }
        et.OnEyeTrackingEvent -= CheckEyeTrackingForProductHit;
    }
    void Update()
    {
        duration += Time.deltaTime; // Update timer
        navMeshAgent.destination = movePositionTransform.position;
        animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
    }
}
