using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RealtimeGenericDictTest : MonoBehaviour
{
    public int length = 1;

    private RealtimeFacialSync _realtimeDict;

    void Start()
    {
        _realtimeDict = GetComponent<RealtimeFacialSync>();
        NewRandomDict();
    }


    public void NewRandomDict()
    {
        _realtimeDict.Reset();
        for (uint k = 0; k < length; k++)
        {
            var v = Random.Range(0.1f, 1.0f);
            _realtimeDict.SetValue(k, v, RealtimeFacialSync.SyncDictType.LIP);
        }
        Debug.Log("New dict created");
    }

    public void PrintDict()
    {
        _realtimeDict.Print();
    }
}