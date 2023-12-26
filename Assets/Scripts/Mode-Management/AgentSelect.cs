using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;
using Microsoft.Win32.SafeHandles;

public class AgentSelect : MonoBehaviour
{
    public GameObject agentSpawn;
    public XROrigin xrOrigin;
    public Savefile savefile;
    void Start()
    {
        // Teleports agent to spawn point if it is not the Varjo HMD
        this.EnsureObjectReference(ref agentSpawn, "Agent Spawn");
        this.EnsureComponentReference(ref xrOrigin);
        this.EnsureComponentReference(ref savefile, "SavefileManager");

        if (savefile.avatarInput != AvatarInput.VARJO)
        {
            xrOrigin = GetComponent<XROrigin>();
            Debug.Log("Teleporting agent to spawn.");
            var heightAdjustment = xrOrigin.Origin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
            var cameraDestination = agentSpawn.transform.position + heightAdjustment;

            xrOrigin.MoveCameraToWorldLocation(cameraDestination);
         }
    }
}
