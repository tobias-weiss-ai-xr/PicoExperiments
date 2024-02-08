using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using TMPro;

public class RealtimeDoor : RealtimeComponent<RealtimeDoorModel>
{
    public GameObject doorIndicator;
    public GameObject clientTextIndicator;
    void Awake()
    {
        if (doorIndicator == null)
            doorIndicator = GameObject.Find("DoorIndicator");
        if (clientTextIndicator == null)
            clientTextIndicator = GameObject.Find("DoorClientText");
    }

    protected override void OnRealtimeModelReplaced(RealtimeDoorModel previousModel, RealtimeDoorModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.openDidChange -= OpenDidChange;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with the default door state
            if (currentModel.isFreshModel)
                currentModel.open = false;

            OpenDidChange(currentModel, currentModel.open);
            // Register for events so we'll know if the color changes later
            currentModel.openDidChange += OpenDidChange;
        }
    }

    private void OpenDidChange(RealtimeDoorModel model, bool open)
    {
        UpdateOpenStatus(open);
    }

    private void UpdateOpenStatus(bool doorOpen)
    {
        gameObject.SetActive(!doorOpen);
        Debug.Log($"Door status changed to: {!doorOpen}");
        if (!doorOpen)
        {
            doorIndicator.GetComponent<TMP_Text>().text = "Door is closed!";
            clientTextIndicator.GetComponent<TMP_Text>().text = "<b>The sales agent is<br> soon at your service!</b>";
        }
        else
        {
            doorIndicator.GetComponent<TMP_Text>().text = "Door is open!";
            clientTextIndicator.GetComponent<TMP_Text>().text = "<b>Feel free to ask<br> questions to our agent!</b>";
        }
    }

    public void ToggleDoor()
    {
        model.open = !model.open;
    }
}
