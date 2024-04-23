using System.Collections;
using Convai.Scripts;
using Convai.Scripts.Utils;
using UnityEngine;

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
        agent.HandleInputSubmission("Begrüße den Benutzenden exakt mit den Worten:" + 
                                    "Hallo, ich bin ihr KI-gesteuerter Assistent. Sie können mir gerne Fragen zu den Produkten und ihrer Nutzung stellen. Ich kann für Sie ein Beispielprodukt drucken und bin Ihnen außerdem gerne beim Abschluss des Einkaufs behilflich."
                                    );
    }
}
