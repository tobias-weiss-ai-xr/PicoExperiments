using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckoutUISync : RealtimeComponent<RealtimeCheckoutModel>
{
    public Button purchaseButton;
    public TMP_Dropdown purchaseDropdown;

    public void Awake()
    {
        this.EnsureComponentReferenceInChildren(ref purchaseButton, "Purchase Button");
        this.EnsureComponentReferenceInChildren(ref purchaseDropdown, "Purchase Dropdown");
    }

    public void UpdateDropdownIndex(RealtimeCheckoutModel model, int value)
    {
        // Get the color from the model and set it on the mesh renderer.
        purchaseDropdown.value = value;
    }

    public void UpdateButtonPressed(RealtimeCheckoutModel model, bool value)
    {
        //TODO: Update button
        if (value)
        {
            purchaseButton.onClick.Invoke();
        }
    }

    protected override void OnRealtimeModelReplaced(RealtimeCheckoutModel previousModel, RealtimeCheckoutModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.buttonPressedDidChange -= UpdateButtonPressed;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with the current mesh renderer color.
            if (currentModel.isFreshModel)
            {
                currentModel.buttonPressed = false;
                currentModel.selectedIndex = purchaseDropdown.value;
            }
            UpdateButtonPressed(currentModel, currentModel.buttonPressed);
            UpdateDropdownIndex(currentModel, currentModel.selectedIndex);
            // Register for events so we'll know if the color changes later
            currentModel.selectedIndexDidChange += UpdateDropdownIndex;
            currentModel.buttonPressedDidChange += UpdateButtonPressed;
        }
    }

    public void SetSelectedIndex(int newIndex)
    {
        model.selectedIndex = newIndex;
    }

    public void SetButtonPressed(bool newState)
    {
        model.buttonPressed = newState;
    }

}
