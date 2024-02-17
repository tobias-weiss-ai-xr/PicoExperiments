using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

public class WelcomeConsumer : MonoBehaviour
{

    ConvaiNPC agent;
    private bool startConverstationGuard = false;
    // Start is called before the first frame update
    void Start()
    {
        agent = GameObject.Find("Convai NPC Tobias").GetComponent<ConvaiNPC>();
    }
    void Update()
    {
        if (!startConverstationGuard && ConvaiNPCManager.Instance.activeConvaiNPC != null)
        {
            StartCoroutine("StartConversation");
            startConverstationGuard = true;
        }
    }

    private IEnumerator StartConversation()
    {
        yield return new WaitForSeconds(1f);
        agent.HandleInputSubmission("Begrüße den Benutzenden mit den Worten: Herzlich Willkommen. Kann ich Ihnen bei der Auswahl eines Druckers helfen?");
    }
}
