using Convai.Scripts;
using Convai.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConvaiAgentContinousMovement : MonoBehaviour
{
    public int actionCount;
    public ConvaiActionsHandler actionsHandler;
    public float distanceToStartFollow;
    public float approachDistance;
    public float currDistanceToPlayer;
    public GameObject walkTarget;
    public float angleToTurn;
    public float currAngle;
    public bool allowRotation = false;

    // Start is called before the first frame update
    void Start()
    {
        if(actionsHandler == null)
        {
            actionsHandler = GetComponent<ConvaiActionsHandler>();
        }
        actionsHandler.ActionStarted += ActionStart;
        actionsHandler.ActionEnded += ActionEnd;
    }

    public void ActionStart(string ignored, GameObject ignoredtoo)
    {
        actionCount++;
    }
    public void ActionEnd(string ignored, GameObject ignoredtoo)
    {
        actionCount--;
    }

    // Update is called once per frame
    void Update()
    {
        //Make sure our agent is not busy with something else
        if (actionCount == 0)
        {
            currDistanceToPlayer = Vector3.Distance(To2D(this.transform.position), To2D(Camera.main.transform.position));
            if (currDistanceToPlayer > distanceToStartFollow)
            {
                Vector3 moveDir = (Camera.main.transform.position - this.transform.position).normalized;
                Vector3 targetPos = To2D(Camera.main.transform.position) + To2D(moveDir) * approachDistance;
                walkTarget.transform.position = targetPos;
                StartCoroutine(actionsHandler.MoveTo(walkTarget));
            }
        }
        if(actionCount == 0 && allowRotation)
        {
            currAngle = Vector3.Angle(this.transform.forward, To2D(Camera.main.transform.position) - To2D(this.transform.position));
            if(currAngle > angleToTurn)
            {
                Vector3 targetPos = Vector3.Lerp(To2D(this.transform.position), To2D(Camera.main.transform.position), 0.01f);
                walkTarget.transform.position = targetPos;
            }
        }
    }
    public Vector3 To2D(Vector3 input)
    {
        return new Vector3(input.x, 0, input.z);
    }
}
