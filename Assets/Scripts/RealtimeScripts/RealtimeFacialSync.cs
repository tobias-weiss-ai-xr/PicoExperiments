using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

public class RealtimeFacialSync : RealtimeComponent<RealtimeFacialModel>
{

    public void Reset()
    {
        foreach (KeyValuePair<uint, RealtimeGenericFloatValueModel> entry in model.entries)
        {
            model.entries.Remove(entry.Key);
        }
        foreach (KeyValuePair<uint, RealtimeGenericFloatValueModel> entry in model.entries)
        {
            model.eyeEntries.Remove(entry.Key);
        }
    }
    public RealtimeFacialModel GetModel()
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

    public float GetValue(uint key, SyncDictType eyeOrLip)
    {
        if (!KeyExistsInDict(key, eyeOrLip))
            return 0.0f;

        if (eyeOrLip == SyncDictType.EYE)
        {
            return model.eyeEntries[key].value;
        }
        else if (eyeOrLip == SyncDictType.LIP) //Redundant atm, but easier to extend in the future
        {
            return model.entries[key].value;
        }
        Debug.LogError("Requested value for unavailable dict type");
        return -1f;
    }

    public void SetValue(uint key, float value, SyncDictType eyeOrLip)
    {
        if (!KeyExistsInDict(key, eyeOrLip)) 
        {
            AddKeyValueToDict(key, value, eyeOrLip);
        }
        else
        {
            if (eyeOrLip == SyncDictType.EYE)
            {
                model.eyeEntries[key].value = value;
            }
            else if (eyeOrLip == SyncDictType.LIP) //Redundant atm, but easier to extend in the future
            {
                model.entries[key].value = value;
            }
            
        }
    }

    private void AddKeyValueToDict(uint key, float value, SyncDictType eyeOrLip)
    {
        RealtimeGenericFloatValueModel newFloat = new();
        newFloat.value = value;
        if(eyeOrLip == SyncDictType.EYE)
        {
            model.eyeEntries.Add(key, newFloat);
        } else if(eyeOrLip == SyncDictType.LIP) //Redundant atm, but easier to extend in the future
        {
            model.entries.Add(key, newFloat);
        }
       
    }

    private bool KeyExistsInDict(uint key, SyncDictType eyeOrLip)
    {
        if(eyeOrLip == SyncDictType.EYE)
        {
            return model.eyeEntries.ContainsKey(key);
        } else if (eyeOrLip == SyncDictType.LIP)
        {
            return model.entries.ContainsKey(key);
        }
        return false;
    }
    protected override void OnRealtimeModelReplaced(RealtimeFacialModel previousModel, RealtimeFacialModel currentModel)
    {
        if (previousModel != null)
        {
            // Unregister from events
            previousModel.entries.modelAdded -= ValueAddedLip;
            previousModel.entries.modelReplaced -= ValueChangedLip;
            previousModel.entries.modelRemoved -= ValueRemovedLip;
            previousModel.eyeEntries.modelAdded -= ValueAddedEye;
            previousModel.eyeEntries.modelReplaced -= ValueChangedEye;
            previousModel.eyeEntries.modelRemoved -= ValueRemovedEye;
        }

        if (currentModel != null)
        {
            // Let us know when a new ribbon point is added to the mesh
            currentModel.entries.modelAdded += ValueAddedLip;
            currentModel.entries.modelReplaced += ValueChangedLip;
            currentModel.entries.modelRemoved += ValueRemovedLip;
            currentModel.eyeEntries.modelAdded += ValueAddedEye;
            currentModel.eyeEntries.modelReplaced += ValueChangedEye;
            currentModel.eyeEntries.modelRemoved += ValueRemovedEye;
        }
    }

    public Vector3 GetGaze()
    {
        return model.eyeGazeDirection;
    }

    public void SetGaze(Vector3 gazeDir)
    {
        model.eyeGazeDirection += gazeDir;
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
    private void ValueAddedEye(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel model, bool remote)
    {
        Debug.Log("Value added - EYE");
    }
    private void ValueChangedEye(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel oldModel, RealtimeGenericFloatValueModel newModel, bool remote)
    {
        Debug.Log("Value changed - EYE");
    }
    private void ValueRemovedEye(RealtimeDictionary<RealtimeGenericFloatValueModel> dict, uint key, RealtimeGenericFloatValueModel model, bool remote)
    {
        Debug.Log("Value removed - EYE");
    }

    public enum SyncDictType
    {
        EYE, LIP
    }
}