using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using TMPro;

public class RealtimeDashboard3DPrinter : RealtimeComponent<RealtimeDashboardModel3DPrinter>
{
    private TextMeshProUGUI _textExplorer;
    private TextMeshProUGUI _textSolid;
    private TextMeshProUGUI _textPlus;
    private TextMeshProUGUI _textPro;
    private void Awake()
    {
        _textExplorer = GameObject.Find("TextExplorer").GetComponent<TextMeshProUGUI>();
        _textSolid = GameObject.Find("TextSolid").GetComponent<TextMeshProUGUI>();
        _textPlus = GameObject.Find("TextPlus").GetComponent<TextMeshProUGUI>();
        _textPro = GameObject.Find("TextPro").GetComponent<TextMeshProUGUI>();
    }

    protected override void OnRealtimeModelReplaced(RealtimeDashboardModel3DPrinter previousModel, RealtimeDashboardModel3DPrinter currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.valueExplorerDidChange -= ValueExplorerChange;
            previousModel.valueSolidDidChange -= ValueSolidChange;
            previousModel.valuePlusDidChange -= ValuePlusChange;
            previousModel.valueProDidChange -= ValueProChange;
        }

        if (currentModel != null)
        {
            // If this is a model that has no data set on it, populate it with the current mesh renderer color.
            if (currentModel.isFreshModel)
                currentModel.valueExplorer = 0.0f;

            // Update the mesh render to match the new model
            UpdateValueExplorer();
            UpdateValueSolid();
            UpdateValuePlus();
            UpdateValuePro();

            // Register for events so we'll know if the color changes later
            currentModel.valueExplorerDidChange += ValueExplorerChange;
            currentModel.valueSolidDidChange += ValueSolidChange;
            currentModel.valuePlusDidChange += ValuePlusChange;
            currentModel.valueProDidChange += ValueProChange;
        }
    }


    private void ValueExplorerChange(RealtimeDashboardModel3DPrinter model, float value)
    {
        UpdateValueExplorer();
    }
    private void UpdateValueExplorer()
    {
        _textExplorer.text = model.valueExplorer.ToString("F2");

    }
    public void SetValueExplorer(float value)
    {
        model.valueExplorer = value;
    }

    private void ValueSolidChange(RealtimeDashboardModel3DPrinter model, float value)
    {
        UpdateValueSolid();
    }
    private void UpdateValueSolid()
    {
        _textSolid.text = model.valueSolid.ToString("F2");

    }
    public void SetValueSolid(float value)
    {
        model.valueSolid = value;
    }

    private void ValuePlusChange(RealtimeDashboardModel3DPrinter model, float value)
    {
        UpdateValuePlus();
    }
    private void UpdateValuePlus()
    {
        _textPlus.text = model.valuePlus.ToString("F2");

    }
    public void SetValuePlus(float value)
    {
        model.valuePlus = value;
    }

    private void ValueProChange(RealtimeDashboardModel3DPrinter model, float value)
    {
        UpdateValuePro();
    }
    private void UpdateValuePro()
    {
        _textPro.text = model.valuePro.ToString("F2");

    }
    public void SetValuePro(float value)
    {
        model.valuePro = value;
    }
}
