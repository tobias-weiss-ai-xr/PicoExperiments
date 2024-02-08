using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using TMPro;

public class RealtimeDashboard : RealtimeComponent<RealtimeDashboardModel>
{
    private TextMeshProUGUI _textAriel;
    private TextMeshProUGUI _textKuschelweich;
    private TextMeshProUGUI _textOmo;
    private TextMeshProUGUI _textWeisserRiese;
    private void Awake()
    {
        _textAriel = GameObject.Find("TextAriel").GetComponent<TextMeshProUGUI>();
        _textKuschelweich = GameObject.Find("TextKuschelweich").GetComponent<TextMeshProUGUI>();
        _textOmo = GameObject.Find("TextOmo").GetComponent<TextMeshProUGUI>();
        _textWeisserRiese = GameObject.Find("TextWeisserRiese").GetComponent<TextMeshProUGUI>();
    }

    protected override void OnRealtimeModelReplaced(RealtimeDashboardModel previousModel, RealtimeDashboardModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.valueArielDidChange -= ValueArielChange;
            previousModel.valueKuschelweichDidChange -= ValueKuschelweichChange;
            previousModel.valueOmoDidChange -= ValueOmoChange;
            previousModel.valueWeisserRieseDidChange -= ValueWeisserRieseChange;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with 0.0f
            if (currentModel.isFreshModel)
            {
                currentModel.valueAriel = 0.0f;
                currentModel.valueKuschelweich = 0.0f;
                currentModel.valueOmo = 0.0f;
                currentModel.valueWeisserRiese = 0.0f;
            }

            // Update the mesh render to match the new model
            UpdateValueAriel();
            UpdateValueKuschelweich();
            UpdateValueOmo();
            UpdateValueWeisserRiese();

            // Register for events so we'll know if the color changes later
            currentModel.valueArielDidChange += ValueArielChange;
            currentModel.valueKuschelweichDidChange += ValueKuschelweichChange;
            currentModel.valueOmoDidChange += ValueOmoChange;
            currentModel.valueWeisserRieseDidChange += ValueWeisserRieseChange;
        }
    }


    private void ValueArielChange(RealtimeDashboardModel model, float value)
    {
        UpdateValueAriel();
    }
    private void UpdateValueAriel()
    {
        _textAriel.text = model.valueAriel.ToString("F2");

    }
    public void SetValueAriel(float value)
    {
        model.valueAriel = value;
    }

    private void ValueKuschelweichChange(RealtimeDashboardModel model, float value)
    {
        UpdateValueKuschelweich();
    }
    private void UpdateValueKuschelweich()
    {
        _textKuschelweich.text = model.valueKuschelweich.ToString("F2");

    }
    public void SetValueKuschelweich(float value)
    {
        model.valueKuschelweich = value;
    }

    private void ValueOmoChange(RealtimeDashboardModel model, float value)
    {
        UpdateValueOmo();
    }
    private void UpdateValueOmo()
    {
        _textOmo.text = model.valueOmo.ToString("F2");

    }
    public void SetValueOmo(float value)
    {
        model.valueOmo = value;
    }

    private void ValueWeisserRieseChange(RealtimeDashboardModel model, float value)
    {
        UpdateValueWeisserRiese();
    }
    private void UpdateValueWeisserRiese()
    {
        _textWeisserRiese.text = model.valueWeisserRiese.ToString("F2");

    }
    public void SetValueWeisserRiese(float value)
    {
        model.valueWeisserRiese = value;
    }
}

