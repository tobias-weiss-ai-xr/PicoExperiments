using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

public class RealtimeButton : RealtimeComponent<RealtimeButtonModel>
{
    public GameObject printer;
    private void Awake()
    {
    }

    protected override void OnRealtimeModelReplaced(RealtimeButtonModel previousModel, RealtimeButtonModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.buttonPressedDidChange -= ButtonDidChange;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with the current mesh renderer color.
            if (currentModel.isFreshModel)
                currentModel.buttonPressed = false;
            
            // UpdateButtonPress();

            // Register for events so we'll know if the state changes later
            currentModel.buttonPressedDidChange += ButtonDidChange;
        }
    }

    private void ButtonDidChange(RealtimeButtonModel model, bool status)
    {
        UpdateButtonPress();
    }

    private void UpdateButtonPress()
    {
        string nameofButton = gameObject.transform.parent.name;
        print(nameofButton + " pressed: " + model.buttonPressed);
        //_worldButton.OnPress.Invoke(); // Trigger animation and object spawn
        printer.GetComponent<TriggerAnimation>().Run();
    }
    public void ClickButton()
    {
        model.buttonPressed = model.buttonPressed ? false : true; // flip flop button state 
    }
}
