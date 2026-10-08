using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Normal.Realtime;
using Normal.Realtime.Serialization;

public class RealtimeVisemeSync : RealtimeComponent<RealtimeVisemeModel>
{
    public SkinnedMeshRenderer skinnedMeshRenderer;

    // Update is called once per frame
    void Update()
    {
        SetVisemes();
    }

    void SetVisemes()
    {
        // Iterate the actual keys: blendshape index == dictionary key.
        // A count-based loop silently misassigns weights when keys are non-contiguous.
        foreach (KeyValuePair<uint, RealtimeGenericFloatValueModel> entry in model.entries)
        {
            // skip index 0 (silent viseme; looks weird if set to the default value of 100)
            if (entry.Key == 0)
                continue;
            skinnedMeshRenderer.SetBlendShapeWeight((int)entry.Key, entry.Value.value);
        }
    }

    public void Reset()
    {
        // Snapshot keys: removing entries while enumerating the dictionary throws
        foreach (KeyValuePair<uint, RealtimeGenericFloatValueModel> entry in model.entries.ToArray())
        {
            model.entries.Remove(entry.Key);
        }
    }
    public RealtimeVisemeModel GetModel()
    {
        return model;
    }

    public void Print()
    {
        foreach (KeyValuePair<uint, RealtimeGenericFloatValueModel> entry in model.entries)
        {
            Debug.Log($"{entry.Key}:{entry.Value.value}");
        }
    }

    public float GetValue(uint key)
    {
        if (!KeyExistsInDict(key))
            return 0.0f;
        return model.entries[key].value;
    }

    public void SetValue(uint key, float value)
    {
        if (!KeyExistsInDict(key))
        {
            AddKeyValueToDict(key, value);
        }
        else
        {
            model.entries[key].value = value;
        }
    }

    private void AddKeyValueToDict(uint key, float value)
    {
        RealtimeGenericFloatValueModel newFloat = new();
        newFloat.value = value;
        model.entries.Add(key, newFloat);
    }

    private bool KeyExistsInDict(uint key)
    {
        return model.entries.ContainsKey(key);
    }
    protected override void OnRealtimeModelReplaced(RealtimeVisemeModel previousModel, RealtimeVisemeModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.entries.modelAdded -= ValueAddedLip;
            previousModel.entries.modelReplaced -= ValueChangedLip;
            previousModel.entries.modelRemoved -= ValueRemovedLip;
        }

        if (currentModel != null)
        {
            // Let us know when a new ribbon point is added to the mesh
            currentModel.entries.modelAdded += ValueAddedLip;
            currentModel.entries.modelReplaced += ValueChangedLip;
            currentModel.entries.modelRemoved += ValueRemovedLip;
        }
    }

    private void ValueAddedLip(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel model, bool remote)
    {
        Debug.Log("Value added - LIP");
    }
    private void ValueChangedLip(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel oldModel, RealtimeGenericFloatValueModel newModel, bool remote)
    {
        Debug.Log("Value changed - LIP");
    }
    private void ValueRemovedLip(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel model, bool remote)
    {
        Debug.Log("Value removed - LIP");
    }
}
