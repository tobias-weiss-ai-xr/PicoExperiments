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
                                    "Ich beantworte gerne alle Fragen zu den Druckern und helfe Ihnen bei der Entscheidung." + 
                                    "Ich kann für Sie ein Beispielobjekt drucken und bin Ihnen gerne beim Abschluss des Einkaufs behilflich." +
                                    "Drücken Sie den Abzug-Knopf mit dem Zeigefinger um mit mir zu sprechen." 
                                    );
    }
}
